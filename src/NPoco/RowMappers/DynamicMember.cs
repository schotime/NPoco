using System;
using System.Collections.Generic;
using System.Reflection;

namespace NPoco.RowMappers
{
//    public class DynamicMember : MemberInfo
//    {
//        public static DynamicMember Create(string name)
//        {
//            var member = new DynamicMember();
//            member.SetName(name);
//            return member;
//        }

//        private string _name;

//        public void SetName(string name)
//        {
//            _name = name;
//        }

//        public override string Name { get { return _name; } }
//        public override Type DeclaringType { get { return typeof(IDictionary<string, object>); } }
//        public Type DynamicType { get { return typeof(object); } }

//        public object GetValue(object target)
//        {
//            object val;
//            ((IDictionary<string, object>)target).TryGetValue(Name, out val);
//            return val;
//        }

//#if !DNXCORE50
//        public override Type ReflectedType { get { return DynamicType; } }

//        public override bool IsDefined(Type attributeType, bool inherit)
//        {
//            throw new NotImplementedException();
//        }

//        public override MemberTypes MemberType { get { return MemberTypes.Custom; } }

//        public override object[] GetCustomAttributes(bool inherit)
//        {
//            throw new NotImplementedException();
//        }

//        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
//        {
//            throw new NotImplementedException();
//        }
//#endif
//    }
}