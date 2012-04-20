using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentMappingConfiguration
    {
        public static void Configure(params IPetaPocoMap[] petaPocoMaps)
        {
            var mappings = PetaPocoMappings.BuildMappingsFromMaps(petaPocoMaps);
            SetFactory(mappings, null);
        }

        public static void Configure(PetaPocoMappings mappings)
        {
            SetFactory(mappings, null);
        }

        public static PetaPocoMappings Scan(Action<IPetaPocoConventionScanner> scanner)
        {
            var scannerSettings = ProcessSettings(scanner);
            if (scannerSettings.Lazy)
            {
                var lazyPetaPocoMappings = new PetaPocoMappings();
                SetFactory(lazyPetaPocoMappings, scanner);
                return lazyPetaPocoMappings;
            }
            
            return CreateMappings(scannerSettings, null);
        }

        private static PetaPocoMappings CreateMappings(PetaPocoConventionScannerSettings scannerSettings, Type[] typesOverride)
        {
            var types = typesOverride ?? FindTypes(scannerSettings);

            var config = new Dictionary<Type, PetaPocoTypeDefinition>();

            foreach (var type in types)
            {
                var petaPocoDefn = new PetaPocoTypeDefinition(type)
                {
                    AutoIncrement = scannerSettings.PrimaryKeysAutoIncremented(type),
                    PrimaryKey = scannerSettings.PrimaryKeysNamed(type),
                    TableName = scannerSettings.TablesNamed(type),
                    SequenceName = scannerSettings.SequencesNamed(type),
                };

                foreach (var prop in type.GetProperties())
                {
                    var column = new PetaPocoColumnDefinition();
                    column.PropertyInfo = prop;
                    column.DbColumnName = scannerSettings.PropertiesNamed(prop);
                    column.IgnoreColumn = scannerSettings.IgnorePropertiesWhere.Any(x => x.Invoke(prop));
                    column.ResultColumn = scannerSettings.ResultPropertiesWhere(prop);
                    column.VersionColumn = scannerSettings.VersionPropertiesWhere(prop);
                    petaPocoDefn.ColumnConfiguration.Add(prop.Name, column);
                }

                config.Add(type, petaPocoDefn);
            }

            MergeOverrides(config, scannerSettings.MappingOverrides);

            var petaPocoMappings = new PetaPocoMappings {Config = config};
            SetFactory(petaPocoMappings, null);
            return petaPocoMappings;
        }

        private static PetaPocoConventionScannerSettings ProcessSettings(Action<IPetaPocoConventionScanner> scanner)
        {
            var defaultScannerSettings = new PetaPocoConventionScannerSettings
            {
                PrimaryKeysAutoIncremented = x => true,
                PrimaryKeysNamed = x => "ID",
                TablesNamed = x => x.Name,
                PropertiesNamed = x => x.Name,
                ResultPropertiesWhere = x => false,
                VersionPropertiesWhere = x => false,
                SequencesNamed = x => null,
                Lazy = false
            };

            scanner.Invoke(new PetaPocoConventionScanner(defaultScannerSettings));
            return defaultScannerSettings;
        }

        private static IEnumerable<Type> FindTypes(PetaPocoConventionScannerSettings scannerSettings)
        {
            if (scannerSettings.TheCallingAssembly)
                scannerSettings.Assemblies.Add(FindTheCallingAssembly());

            var types = scannerSettings.Assemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => scannerSettings.IncludeTypes.All(y => y.Invoke(x)))
                .Where(x => !x.IsNested && !typeof (PetaPocoMap<>).IsAssignableFrom(x) && !typeof (PetaPocoMappings).IsAssignableFrom(x));
            return types;
        }

        private static void MergeOverrides(Dictionary<Type, PetaPocoTypeDefinition> config, PetaPocoMappings overrideMappings)
        {
            if (overrideMappings == null)
                return;

            foreach (var overrideTypeDefinition in overrideMappings.Config)
            {
                if (!config.ContainsKey(overrideTypeDefinition.Key))
                    continue;

                var convTableDefinition = config[overrideTypeDefinition.Key];

                convTableDefinition.PrimaryKey = overrideTypeDefinition.Value.PrimaryKey ?? convTableDefinition.PrimaryKey;
                convTableDefinition.SequenceName = overrideTypeDefinition.Value.SequenceName ?? convTableDefinition.SequenceName;
                convTableDefinition.TableName = overrideTypeDefinition.Value.TableName ?? convTableDefinition.TableName;
                convTableDefinition.AutoIncrement = overrideTypeDefinition.Value.AutoIncrement ?? convTableDefinition.AutoIncrement;
                convTableDefinition.ExplicitColumns = overrideTypeDefinition.Value.ExplicitColumns ?? convTableDefinition.ExplicitColumns;

                foreach (var overrideColumnDefinition in overrideMappings.Config[overrideTypeDefinition.Key].ColumnConfiguration)
                {
                    var convColDefinition = convTableDefinition.ColumnConfiguration[overrideColumnDefinition.Key];

                    convColDefinition.DbColumnName = overrideColumnDefinition.Value.DbColumnName ?? convColDefinition.DbColumnName;
                    convColDefinition.IgnoreColumn = overrideColumnDefinition.Value.IgnoreColumn ?? convColDefinition.IgnoreColumn;
                    convColDefinition.ResultColumn = overrideColumnDefinition.Value.ResultColumn ?? convColDefinition.ResultColumn;
                    convColDefinition.VersionColumn = overrideColumnDefinition.Value.VersionColumn ?? convColDefinition.VersionColumn;
                    convColDefinition.PropertyInfo = overrideColumnDefinition.Value.PropertyInfo ?? convColDefinition.PropertyInfo;    
                }
            }
        }

        private static void SetFactory(PetaPocoMappings mappings, Action<IPetaPocoConventionScanner> scanner)
        {
            var maps = mappings;
            var scana = scanner;
            Database.PocoDataFactory = t =>
            {
                if (maps != null)
                {
                    if (maps.Config.ContainsKey(t))
                    {
                        return new FluentMappingsPocoData(t, mappings.Config[t]);
                    }

                    if (scana != null)
                    {
                        var settings = ProcessSettings(scana);
                        var typeMapping = CreateMappings(settings, new[] { t });
                        return new FluentMappingsPocoData(t, typeMapping.Config[t]);
                    }
                }
                return new Database.PocoData(t);
            };
        }

        // Helper method if code is in seperate assembly
        private static Assembly FindTheCallingAssembly()
        {
            if (!typeof(FluentMappingConfiguration).Assembly.FullName.StartsWith("PetaPoco,"))
                return Assembly.GetCallingAssembly();

            var trace = new StackTrace(false);

            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            Assembly callingAssembly = null;
            for (int i = 0; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                Assembly assembly = frame.GetMethod().DeclaringType.Assembly;
                if (assembly != thisAssembly)
                {
                    callingAssembly = assembly;
                    break;
                }
            }
            return callingAssembly;
        }
    }
}