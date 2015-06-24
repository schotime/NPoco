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

        public string Name { get { return MemberInfo.Name; } }
        public MemberInfo MemberInfo { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public List<PocoMember> PocoMemberChildren { get; set; }

        public ReferenceMappingType ReferenceMappingType { get; set; }
        public string ReferenceMemberName { get; set; }

        public bool IsList { get; set; }

        private FastCreate _creator;
        private MemberAccessor _memberAccessor;
        private Type _listType;

        public object Create()
        {
            return _creator.Create();
        }

        public IList CreateList()
        {
            //var listType = typeof(List<>).MakeGenericType(MemberType.GetGenericArguments().First());
            var list = Activator.CreateInstance(_listType);
            return (IList) list;
        }

        public void SetMemberAccessor(MemberAccessor memberAccessor, FastCreate fastCreate, Type listType)
        {
            _listType = listType;
            _memberAccessor = memberAccessor;
            _creator = fastCreate;
        }

        public void SetValue(object target, object value)
        {
            _memberAccessor.Set(target, value);
        }

        public object GetValue(object target)
        {
            return _memberAccessor.Get(target);
        }
    }
}