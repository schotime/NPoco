using System;
using System.Reflection;

namespace NPoco
{
    public class PocoColumn
    {
        public string ColumnName;
        public PropertyInfo PropertyInfo;
        public bool ResultColumn;
        public bool VersionColumn;
        private Type _columnType;
        public Type ColumnType
        {
            get { return _columnType ?? PropertyInfo.PropertyType; }
            set { _columnType = value; }
        }

        public bool ForceToUtc { get; set; }

        public virtual void SetValue(object target, object val) { PropertyInfo.SetValue(target, val, null); }
        public virtual object GetValue(object target) { return PropertyInfo.GetValue(target, null); }
        public virtual object ChangeType(object val) { return Convert.ChangeType(val, PropertyInfo.PropertyType); }
    }
}