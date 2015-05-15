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

        public TableInfo TableInfo;
        public string ColumnName;

        public MemberInfo MemberInfo
        {
            get { return _memberInfo; }
            set { 
                _memberInfo = value; 
                SetupMemberAccessor(); 
            }
        }

        public bool ResultColumn;
        public bool VersionColumn;
        public VersionColumnType VersionColumnType;
        public bool ComputedColumn;
        private Type _columnType;
        private MemberAccessor _memberAccessor;
        private MemberInfo _memberInfo;

        public Type ColumnType
        {
            get { return _columnType ?? MemberInfo.GetMemberInfoType(); }
            set { _columnType = value; }
        }

        public bool ForceToUtc { get; set; }
        public string AutoAlias { get; set; }
        public string ColumnAlias { get; set; }

        public virtual void SetValue(object target, object val) { MemberInfo.SetMemberInfoValue(target, val); }
        public virtual object GetValue(object target) { return MemberInfo.GetMemberInfoValue(target); }
        public virtual object ChangeType(object val) { return Convert.ChangeType(val, MemberInfo.GetMemberInfoType()); }

        public virtual void SetValueFast(object target, object val)
        {
            _memberAccessor.Set(target, val);
        }

        private void SetupMemberAccessor()
        {
            _memberAccessor = new MemberAccessor(MemberInfo.DeclaringType, MemberInfo.Name);
        }
    }
}