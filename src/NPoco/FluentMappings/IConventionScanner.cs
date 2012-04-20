using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public interface IConventionScanner
    {
        void OverrideMappingsWith(Mappings mappings);
        void OverrideMappingsWith(params IMap[] maps);

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