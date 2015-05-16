using System;
using System.Collections.Generic;
using System.Reflection;

namespace NPoco
{
    public class PocoColumn
    {
        public PocoColumn()
        {
            ForceToUtc = true;
            MemberInfoChain = new List<MemberInfo>();
        }

        public TableInfo TableInfo;
        public string ColumnName;

        public List<MemberInfo> MemberInfoChain { get; set; }
        public MemberInfo MemberInfo
        {
            get { return _memberInfo; }
            set { 
                _memberInfo = value; 
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
        public virtual object GetValue(object target)
        {
            foreach (var memberInfo in MemberInfoChain)
            {
                if (memberInfo == _memberInfo)
                    continue;
                target = target == null ? null : memberInfo.GetMemberInfoValue(target);
            }
            return target == null ? null : MemberInfo.GetMemberInfoValue(target);
        }
        public virtual object ChangeType(object val) { return Convert.ChangeType(val, MemberInfo.GetMemberInfoType()); }

        public virtual void SetValueFast(object target, object val)
        {
            SetupMemberAccessor();
            _memberAccessor.Set(target, val);
        }

        private void SetupMemberAccessor()
        {
            if (_memberAccessor == null)
                _memberAccessor = new MemberAccessor(MemberInfo.DeclaringType, MemberInfo.Name);
        }
    }
}