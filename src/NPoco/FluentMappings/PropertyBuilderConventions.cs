using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class PropertyBuilderConventions : IColumnsBuilderConventions
    {
        private readonly ConventionScannerSettings _scannerSettings;

        public PropertyBuilderConventions(ConventionScannerSettings scannerSettings)
        {
            _scannerSettings = scannerSettings;
        }

        public IColumnsBuilderConventions Named(Func<MemberInfo, string> propertiesNamedFunc)
        {
            _scannerSettings.PropertiesNamed = propertiesNamedFunc;
            return this;
        }

        public IColumnsBuilderConventions IgnoreWhere(Func<MemberInfo, bool> ignorePropertiesWhereFunc)
        {
            _scannerSettings.IgnorePropertiesWhere.Add(ignorePropertiesWhereFunc);
            return this;
        }

        public IColumnsBuilderConventions ResultWhere(Func<MemberInfo, bool> resultPropertiesWhereFunc)
        {
            _scannerSettings.ResultPropertiesWhere = resultPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions VersionWhere(Func<MemberInfo, bool> versionPropertiesWhereFunc)
        {
            _scannerSettings.VersionPropertiesWhere = versionPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions ForceDateTimesToUtcWhere(Func<MemberInfo, bool> forceDateTimesToUtcWhereFunc)
        {
            _scannerSettings.ForceDateTimesToUtcWhere = forceDateTimesToUtcWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions DbColumnTypeAs(Func<MemberInfo, Type> dbColumnTypeAsFunc)
        {
            _scannerSettings.DbColumnTypesAs = dbColumnTypeAsFunc;
            return this;
        }
    }
}