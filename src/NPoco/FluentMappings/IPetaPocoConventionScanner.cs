using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public interface IPetaPocoConventionScanner
    {
        void OverrideMappingsWith(PetaPocoMappings mappings);
        void OverrideMappingsWith(params IPetaPocoMap[] maps);

        void Assembly(Assembly assembly);
        void TheCallingAssembly();
        void IncludeTypes(Func<Type, bool> includeTypes);

        void TablesNamed(Func<Type, string> tableFunc);
        void PrimaryKeysNamed(Func<Type, string> primaryKeyFunc);
        void PrimaryKeysAutoIncremented(Func<Type, bool> primaryKeyAutoIncrementFunc);
        void SequencesNamed(Func<Type, string> sequencesFunc);

        void LazyLoadMappings();

        IColumnsBuilderConventions Columns { get; }
    }
}