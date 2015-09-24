using System;
using System.Collections.Generic;
using System.Reflection;

namespace NPoco.RowMappers
{
    public class DynamicMember : MemberInfo
    {
        private readonly string _name;

        public DynamicMember(string name)
        {
            _name = name;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MemberTypes MemberType { get { return MemberTypes.Custom; } }
        public override string Name { get { return _name; } }
        public override Type DeclaringType { get { return typeof(IDictionary<string, object>); } }
        public override Type ReflectedType { get { return typeof(object); } }

        public object GetValue(object target)
        {
            object val;
            ((IDictionary<string, object>)target).TryGetValue(Name, out val);
            return val;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}