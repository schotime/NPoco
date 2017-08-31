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
            _scannerSettings.DbColumnsNamed = propertiesNamedFunc;
            return this;
        }

        public IColumnsBuilderConventions Aliased(Func<MemberInfo, string> aliasNamedFunc)
        {
            _scannerSettings.AliasNamed = aliasNamedFunc;
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

        public IColumnsBuilderConventions ComputedWhere(Func<MemberInfo, bool> computedPropertiesWhereFunc)
        {
            _scannerSettings.ComputedPropertiesWhere = computedPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions ComputedTypeAs(Func<MemberInfo, ComputedColumnType> computedPropertyTypeAsFunc)
        {
            _scannerSettings.ComputedPropertyTypeAs = computedPropertyTypeAsFunc;
            return this;
        }

        public IColumnsBuilderConventions VersionWhere(Func<MemberInfo, bool> versionPropertiesWhereFunc)
        {
            _scannerSettings.VersionPropertiesWhere = versionPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions VersionTypeAs(Func<MemberInfo, VersionColumnType> versionPropertyTypeAsFunc)
        {
            _scannerSettings.VersionPropertyTypeAs = versionPropertyTypeAsFunc;
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

        public IColumnsBuilderConventions ReferenceNamed(Func<MemberInfo, string> refPropertiesNamedFunc)
        {
            _scannerSettings.ReferenceDbColumnsNamed = refPropertiesNamedFunc;
            return this;
        }

        public IColumnsBuilderConventions ReferencePropertiesWhere(Func<MemberInfo, bool> referencePropertiesWhereFunc)
        {
            _scannerSettings.ReferencePropertiesWhere = referencePropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions ComplexPropertiesWhere(Func<MemberInfo, bool> complexPropertiesWhereFunc)
        {
            _scannerSettings.ComplexPropertiesWhere = complexPropertiesWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions SerializedWhere(Func<MemberInfo, bool> serializedWhereFunc)
        {
            _scannerSettings.SerializedWhere = serializedWhereFunc;
            return this;
        }

        public IColumnsBuilderConventions ValueObjectColumnWhere(Func<MemberInfo, bool> valueObjectColumnWhere)
        {
            _scannerSettings.ValueObjectColumnWhere = valueObjectColumnWhere;
            return this;
        }
    }
}