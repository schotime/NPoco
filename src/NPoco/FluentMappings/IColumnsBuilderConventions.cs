using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public interface IColumnsBuilderConventions
    {
        IColumnsBuilderConventions Named(Func<PropertyInfo, string> propertiesNamedFunc);
        IColumnsBuilderConventions IgnoreWhere(Func<PropertyInfo, bool> ignorePropertiesWhereFunc);
        IColumnsBuilderConventions ResultWhere(Func<PropertyInfo, bool> resultPropertiesWhereFunc);
        IColumnsBuilderConventions VersionWhere(Func<PropertyInfo, bool> versionPropertiesWhereFunc);
        IColumnsBuilderConventions ForceDateTimesToUtcWhere(Func<PropertyInfo, bool> forceDateTimesToUtcWhereFunc);
    }
}