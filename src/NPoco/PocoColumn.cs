using System;
using System.Reflection;

namespace NPoco
{
    public class PocoColumn
    {
        public PocoColumn()
        {
            ForceToUtc = true;
        }

        public string ColumnName;
        public MemberInfo MemberInfo;
        public bool ResultColumn;
        public bool VersionColumn;
        private Type _columnType;
        public Type ColumnType
        {
            get { return _columnType ?? MemberInfo.GetMemberInfoType(); }
            set { _columnType = value; }
        }

        public bool ForceToUtc { get; set; }

        public virtual void SetValue(object target, object val) { MemberInfo.SetMemberInfoValue(target, val); }
        public virtual object GetValue(object target) { return MemberInfo.GetMemberInfoValue(target); }
        public virtual object ChangeType(object val) { return Convert.ChangeType(val, MemberInfo.GetMemberInfoType()); }
    }
}