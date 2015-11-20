using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace NPoco
{
    public abstract class DefaultMapper : IMapper
    {
        public virtual Func<object, object> GetFromDbConverter(MemberInfo destMemberInfo, Type sourceType)
        {
            var type = destMemberInfo.GetMemberInfoType();
            return destMemberInfo != null ? GetFromDbConverter(type, sourceType) : null;
        }

        public virtual Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
        {
            return null;
        }

        public virtual Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
        {
            return null;
        }

        public virtual Func<object, object> GetParameterConverter(DbCommand dbCommand, Type sourceType)
        {
            return null;
        }
    }
}