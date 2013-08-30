using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class FluentConfig
    {
        public FluentConfig(Func<IMapper, Func<Type, PocoData>> config)
        {
            Config = config;
        }

        public Func<IMapper, Func<Type, PocoData>> Config { get; private set; }
    }

    public class FluentMappingConfiguration
    {
        public static FluentConfig Configure(params IMap[] pocoMaps)
        {
            var mappings = Mappings.BuildMappingsFromMaps(pocoMaps);
            return Configure(mappings);
        }

        public static FluentConfig Configure(Mappings mappings)
        {
            return SetFactory(mappings, null);
        }

        public static FluentConfig Scan(Action<IConventionScanner> scanner)
        {
            var scannerSettings = ProcessSettings(scanner);
            if (scannerSettings.Lazy)
            {
                var lazyPocoMappings = new Mappings();
                return SetFactory(lazyPocoMappings, scanner);
            }
            
            return Configure(CreateMappings(scannerSettings, null));
        }

        private static Mappings CreateMappings(ConventionScannerSettings scannerSettings, Type[] typesOverride)
        {
            var types = typesOverride ?? FindTypes(scannerSettings);

            var config = new Dictionary<Type, TypeDefinition>();

            foreach (var type in types)
            {
                var pocoDefn = new TypeDefinition(type)
                {
                    AutoIncrement = scannerSettings.PrimaryKeysAutoIncremented(type),
                    PrimaryKey = scannerSettings.PrimaryKeysNamed(type),
                    TableName = scannerSettings.TablesNamed(type),
                    SequenceName = scannerSettings.SequencesNamed(type),
                    ExplicitColumns = true
                };

                foreach (var prop in ReflectionUtils.GetFieldsAndPropertiesForClasses(type))
                {
                    var column = new ColumnDefinition();
                    column.MemberInfo = prop;
                    column.DbColumnName = scannerSettings.PropertiesNamed(prop);
                    column.IgnoreColumn = scannerSettings.IgnorePropertiesWhere.Any(x => x.Invoke(prop));
                    column.DbColumnType = scannerSettings.DbColumnTypesAs(prop);
                    column.ResultColumn = scannerSettings.ResultPropertiesWhere(prop);
                    column.VersionColumn = scannerSettings.VersionPropertiesWhere(prop);
                    column.ForceUtc = scannerSettings.ForceDateTimesToUtcWhere(prop);
                    pocoDefn.ColumnConfiguration.Add(prop.Name, column);
                }

                config.Add(type, pocoDefn);
            }

            MergeOverrides(config, scannerSettings.MappingOverrides);

            var pocoMappings = new Mappings {Config = config};
            return pocoMappings;
        }

        private static ConventionScannerSettings ProcessSettings(Action<IConventionScanner> scanner)
        {
            var defaultScannerSettings = new ConventionScannerSettings
            {
                PrimaryKeysAutoIncremented = x => true,
                PrimaryKeysNamed = x => "ID",
                TablesNamed = x => x.Name,
                PropertiesNamed = x => x.Name,
                DbColumnTypesAs = x => null,
                ResultPropertiesWhere = x => false,
                VersionPropertiesWhere = x => false,
                ForceDateTimesToUtcWhere = x => true,
                SequencesNamed = x => null,
                Lazy = false
            };

            scanner.Invoke(new ConventionScanner(defaultScannerSettings));
            return defaultScannerSettings;
        }

        private static IEnumerable<Type> FindTypes(ConventionScannerSettings scannerSettings)
        {
            if (scannerSettings.TheCallingAssembly)
                scannerSettings.Assemblies.Add(FindTheCallingAssembly());

            var types = scannerSettings.Assemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => scannerSettings.IncludeTypes.All(y => y.Invoke(x)))
                .Where(x => !x.IsNested && !typeof (Map<>).IsAssignableFrom(x) && !typeof (Mappings).IsAssignableFrom(x));
            return types;
        }

        private static void MergeOverrides(Dictionary<Type, TypeDefinition> config, Mappings overrideMappings)
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
                    convColDefinition.DbColumnType = overrideColumnDefinition.Value.DbColumnType ?? convColDefinition.DbColumnType;
                    convColDefinition.IgnoreColumn = overrideColumnDefinition.Value.IgnoreColumn ?? convColDefinition.IgnoreColumn;
                    convColDefinition.ResultColumn = overrideColumnDefinition.Value.ResultColumn ?? convColDefinition.ResultColumn;
                    convColDefinition.VersionColumn = overrideColumnDefinition.Value.VersionColumn ?? convColDefinition.VersionColumn;
                    convColDefinition.MemberInfo = overrideColumnDefinition.Value.MemberInfo ?? convColDefinition.MemberInfo;
                    convColDefinition.ForceUtc = overrideColumnDefinition.Value.ForceUtc ?? convColDefinition.ForceUtc;
                }
            }
        }

        private static FluentConfig SetFactory(Mappings mappings, Action<IConventionScanner> scanner)
        {
            var maps = mappings;
            var scana = scanner;
            return new FluentConfig(mapper => t =>
            {
                if (maps != null)
                {
                    if (maps.Config.ContainsKey(t))
                    {
                        return new FluentMappingsPocoData(t, mappings.Config[t], mapper);
                    }

                    if (scana != null)
                    {
                        var settings = ProcessSettings(scana);
                        var typeMapping = CreateMappings(settings, new[] { t });
                        return new FluentMappingsPocoData(t, typeMapping.Config[t], mapper);
                    }
                }
                return new PocoData(t, mapper);
            });
        }

        // Helper method if code is in seperate assembly
        private static Assembly FindTheCallingAssembly()
        {
            if (!typeof(FluentMappingConfiguration).Assembly.FullName.StartsWith("NPoco,"))
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