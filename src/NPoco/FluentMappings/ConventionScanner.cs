using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class ConventionScanner : IConventionScanner
    {
        private readonly ConventionScannerSettings _scannerSettings;

        public ConventionScanner(ConventionScannerSettings scannerSettings)
        {
            _scannerSettings = scannerSettings;
        }

        public void OverrideMappingsWith(Mappings mappings)
        {
            _scannerSettings.MappingOverrides.Add(mappings);
        }

        public void OverrideMappingsWith(params IMap[] maps)
        {
            var mappings = Mappings.BuildMappingsFromMaps(maps);
            _scannerSettings.MappingOverrides.Add(mappings);
        }

        public void Assembly(Assembly assembly)
        {
            _scannerSettings.Assemblies.Add(assembly);
        }

#if !DNXCORE50
        public void TheCallingAssembly()
        {
            _scannerSettings.TheCallingAssembly = true;
        }
#endif

        public void IncludeTypes(Func<Type, bool> typeIncludes)
        {
            _scannerSettings.IncludeTypes.Add(typeIncludes);
        }

        public void TablesNamed(Func<Type, string> tableFunc)
        {
            _scannerSettings.TablesNamed = tableFunc;
        }

        public void PrimaryKeysNamed(Func<Type, string> primaryKeyFunc)
        {
            _scannerSettings.PrimaryKeysNamed = primaryKeyFunc;
        }

        public void SequencesNamed(Func<Type, string> sequencesFunc)
        {
            _scannerSettings.SequencesNamed = sequencesFunc;
        }

        public void PersistedTypesBy(Func<Type, Type> persistedTypesByFunc)
        {
            _scannerSettings.PersistedTypesBy = persistedTypesByFunc;
        }

        public void MapNestedTypesWhen(Func<Type, bool> mapNestedTypesFunc)
        {
            _scannerSettings.MapNestedTypesWhen = mapNestedTypesFunc;
        }

        public void LazyLoadMappings()
        {
            _scannerSettings.Lazy = true;
        }

        public void PrimaryKeysAutoIncremented(Func<Type, bool> primaryKeyAutoIncrementFunc)
        {
            _scannerSettings.PrimaryKeysAutoIncremented = primaryKeyAutoIncrementFunc;
        }

        //public void OverrideWithAttributes()
        //{
        //    _scannerSettings.OverrideWithAttributes = true;
        //}

        public IColumnsBuilderConventions Columns
        {
            get { return new PropertyBuilderConventions(_scannerSettings); }
        }
    }
}