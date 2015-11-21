using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class PocoMember
    {
        public PocoMember()
        {
            PocoMemberChildren = new List<PocoMember>();
            ReferenceType = ReferenceType.None;
        }

        public string Name { get { return MemberInfoData.Name; } }
        
        public MemberInfoData MemberInfoData { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public List<PocoMember> PocoMemberChildren { get; set; }

        public ReferenceType ReferenceType { get; set; }
        public string ReferenceMemberName { get; set; }

        public bool IsList { get; set; }
        public bool IsDynamic { get; set; }

        private FastCreate _creator;
        private MemberAccessor _memberAccessor;
        private Type _listType;

        public virtual object Create(DbDataReader dataReader)
        {
            return _creator.Create(dataReader);
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

        public virtual void SetValue(object target, object value)
        {
            _memberAccessor.Set(target, value);
        }

        public virtual object GetValue(object target)
        {
            return _memberAccessor.Get(target);
        }
    }

    public class MemberInfoData : IEquatable<MemberInfoData>
    {
        public MemberInfo MemberInfo { get; private set; }
        public Type DeclaringType { get; private set; }
        public Type MemberType { get; private set; }
        public string Name { get; private set; }

        public MemberInfoData(string name, Type memberType, Type declaringType)
        {
            Name = name;
            MemberType = memberType;
            DeclaringType = declaringType;
        }

        public MemberInfoData(MemberInfo memberInfo)
        {
            MemberInfo = memberInfo;
            Name = memberInfo.Name;
            MemberType = memberInfo.GetMemberInfoType();
            DeclaringType = memberInfo.DeclaringType;
        }

        public bool Equals(MemberInfoData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(MemberType, other.MemberType) && Equals(DeclaringType, other.DeclaringType);
        }

        public static bool operator ==(MemberInfoData left, MemberInfoData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MemberInfoData left, MemberInfoData right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MemberInfoData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (MemberType != null ? MemberType.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (DeclaringType != null ? DeclaringType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}