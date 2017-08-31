using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public interface IConventionScanner
    {
        void OverrideMappingsWith(Mappings mappings);
        void OverrideMappingsWith(params IMap[] maps);

        void Assembly(Assembly assembly);
#if !DNXCORE50
        void TheCallingAssembly();
#endif
        void IncludeTypes(Func<Type, bool> includeTypes);

        void TablesNamed(Func<Type, string> tableFunc);
        void PrimaryKeysNamed(Func<Type, string> primaryKeyFunc);
        void PrimaryKeysAutoIncremented(Func<Type, bool> primaryKeyAutoIncrementFunc);
        void SequencesNamed(Func<Type, string> sequencesFunc);
        void PersistedTypesBy(Func<Type, Type> persistedTypesByFunc);
        void MapNestedTypesWhen(Func<Type, bool> mapNestedTypesFunc);

        void LazyLoadMappings();

        IColumnsBuilderConventions Columns { get; }
    }
}