using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class PocoMember
    {
        public PocoMember()
        {
            PocoMemberChildren = new List<PocoMember>();
            ReferenceMappingType = ReferenceMappingType.None;
        }

        public PocoMember Clone()
        {
            return new PocoMember()
            {
                MemberInfo = MemberInfo,
                PocoMemberChildren = PocoMemberChildren.Select(x => x.Clone()).ToList(),
                ReferenceMappingType = ReferenceMappingType,
                ReferenceMemberName = ReferenceMemberName,
                IsList = IsList,
                PocoColumn = PocoColumn != null ? PocoColumn.Clone() : null,
            };
        }

        public string Name { get { return MemberInfo.Name; } }
        public Type MemberType { get { return MemberInfo.GetMemberInfoType(); } }
        public MemberInfo MemberInfo { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public List<PocoMember> PocoMemberChildren { get; set; }

        public ReferenceMappingType ReferenceMappingType { get; set; }
        public string ReferenceMemberName { get; set; }

        public bool IsList { get; set; }

        private FastCreate _creator;
        public object Create()
        {
            if (_creator == null)
            {
                _creator = new FastCreate(IsList
                                              ? MemberType.GetGenericArguments().First()
                                              : MemberType);
            }

            return _creator.Create();
        }

        public IList CreateList()
        {
            var listType = typeof(List<>).MakeGenericType(MemberType.GetGenericArguments().First());
            var list = Activator.CreateInstance(listType);
            return (IList) list;
        }

        private MemberAccessor _memberAccessor;
        public void SetValue(object target, object value)
        {
            if (_memberAccessor == null)
                _memberAccessor= new MemberAccessor(MemberInfo.DeclaringType, Name);
            _memberAccessor.Set(target, value);
        }
    }
}