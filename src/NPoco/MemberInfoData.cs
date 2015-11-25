using System;
using System.Reflection;

namespace NPoco
{
    public class MemberInfoData : IEquatable<MemberInfoData>
    {
        public MemberInfo MemberInfo
        {
            get;
            private set;
        }

        public Type DeclaringType
        {
            get;
            private set;
        }

        public Type MemberType
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

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
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
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
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((MemberInfoData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MemberType != null ? MemberType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeclaringType != null ? DeclaringType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}