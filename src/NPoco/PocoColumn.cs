using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string GenerateKey(IEnumerable<MemberInfo> memberInfoChain)
        {
            return string.Join("__", memberInfoChain.Select(x => x.Name).ToArray());
        }

        public TableInfo TableInfo;
        public string ColumnName;

        public List<MemberInfo> MemberInfoChain { get; set; }
        public string MemberInfoKey { get { return GenerateKey(MemberInfoChain); } }
        public MemberInfo MemberInfo { get; set; }

        public bool ResultColumn;
        public bool VersionColumn;
        public VersionColumnType VersionColumnType;
        public bool ComputedColumn;
        private Type _columnType;
        private MemberAccessor _memberAccessor;
        private List<MemberAccessor> _memberAccessorChain = new List<MemberAccessor>();

        public Type ColumnType
        {
            get { return _columnType ?? MemberInfo.GetMemberInfoType(); }
            set { _columnType = value; }
        }

        public bool ForceToUtc { get; set; }
        public string ColumnAlias { get; set; }

        public ReferenceMappingType ReferenceMappingType { get; set; }
        public bool ComplexType { get; set; }

        public virtual void SetValue(object target, object val)
        {
            SetValueFast(target, val);
            //MemberInfo.SetMemberInfoValue(target, val);
        }
        public virtual object GetValue(object target)
        {
            SetupMemberAccessorChain();
            foreach (var memberInfo in _memberAccessorChain)
            {
                target = target == null ? null : memberInfo.Get(target);
            }
            //foreach (var memberInfo in MemberInfoChain)
            //{
            //    target = target == null ? null : memberInfo.GetMemberInfoValue(target);
            //}
            return target;
        }

        public virtual object ChangeType(object val) { return Convert.ChangeType(val, MemberInfo.GetMemberInfoType()); }

        public virtual void SetValueFast(object target, object val)
        {
            SetupMemberAccessor();
            _memberAccessor.Set(target, val);
        }

        public virtual object GetValueFast(object target)
        {
            SetupMemberAccessor();
            return _memberAccessor.Get(target);
        }

        private void SetupMemberAccessor()
        {
            if (_memberAccessor == null)
                _memberAccessor = new MemberAccessor(MemberInfo.DeclaringType, MemberInfo.Name);
        }

        private void SetupMemberAccessorChain()
        {
            if (_memberAccessorChain.Count == 0)
            {
                foreach (var memberInfo in MemberInfoChain)
                {
                    _memberAccessorChain.Add(new MemberAccessor(memberInfo.DeclaringType, memberInfo.Name));
                }
            }

            if (_memberAccessor == null)
            {
                _memberAccessor = _memberAccessorChain.Last();
            }
        }
    }
}