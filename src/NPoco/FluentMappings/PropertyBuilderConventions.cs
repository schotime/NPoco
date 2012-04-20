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

        public IColumnsBuilderConventions Named(Func<PropertyInfo, string> propertiesNamedFunc)
        {
            _scannerSettings.PropertiesNamed = propertiesNamedFunc;
            return this;
        }

        public IColumnsBuilderConventions IgnoreWhere(Func<PropertyInfo, bool> ignorePropertiesWhereFunc)
        {
            _scannerSettings.IgnorePropertiesWhere.Add(ignorePropertiesWhereFunc);
            return this;
        }

        public IColumnsBuilderConventions ResultWhere(Func<PropertyInfo, bool> resultPropertiesWhereFunc)
        {
            _scannerSettings.ResultPropertiesWhere = resultPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions VersionWhere(Func<PropertyInfo, bool> versionPropertiesWhereFunc)
        {
            _scannerSettings.VersionPropertiesWhere = versionPropertiesWhereFunc;
            return this;
        }
    }
}