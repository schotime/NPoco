using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class PetaPocoConventionScanner : IPetaPocoConventionScanner
    {
        private readonly PetaPocoConventionScannerSettings _scannerSettings;

        public PetaPocoConventionScanner(PetaPocoConventionScannerSettings scannerSettings)
        {
            _scannerSettings = scannerSettings;
        }

        public void OverrideMappingsWith(PetaPocoMappings mappings)
        {
            _scannerSettings.MappingOverrides = mappings;
        }

        public void OverrideMappingsWith(params IPetaPocoMap[] maps)
        {
            var mappings = PetaPocoMappings.BuildMappingsFromMaps(maps);
            _scannerSettings.MappingOverrides = mappings;
        }

        public void Assembly(Assembly assembly)
        {
            _scannerSettings.Assemblies.Add(assembly);
        }

        public void TheCallingAssembly()
        {
            _scannerSettings.TheCallingAssembly = true;
        }

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

        public void LazyLoadMappings()
        {
            _scannerSettings.Lazy = true;
        }

        public void PrimaryKeysAutoIncremented(Func<Type, bool> primaryKeyAutoIncrementFunc)
        {
            _scannerSettings.PrimaryKeysAutoIncremented = primaryKeyAutoIncrementFunc;
        }

        public IColumnsBuilderConventions Columns
        {
            get { return new PropertyBuilderConventions(_scannerSettings); }
        }
    }
}