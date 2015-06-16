using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public interface IColumnsBuilderConventions
    {
        IColumnsBuilderConventions Named(Func<MemberInfo, string> propertiesNamedFunc);
        IColumnsBuilderConventions Aliased(Func<MemberInfo, string> aliasNamedFunc);
        IColumnsBuilderConventions IgnoreWhere(Func<MemberInfo, bool> ignorePropertiesWhereFunc);
        IColumnsBuilderConventions ResultWhere(Func<MemberInfo, bool> resultPropertiesWhereFunc);
        IColumnsBuilderConventions ComputedWhere(Func<MemberInfo, bool> computedPropertiesWhereFunc);
        IColumnsBuilderConventions VersionWhere(Func<MemberInfo, bool> versionPropertiesWhereFunc);
        IColumnsBuilderConventions ForceDateTimesToUtcWhere(Func<MemberInfo, bool> forceDateTimesToUtcWhereFunc);
        IColumnsBuilderConventions DbColumnTypeAs(Func<MemberInfo, Type> dbColumnTypeAsFunc);
        IColumnsBuilderConventions ReferenceNamed(Func<MemberInfo, string> refPropertiesNamedFunc);
    }
}