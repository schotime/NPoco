using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace NPoco.Tests.Common
{
    public class SqliteMapper : IMapper
    {
        public Func<object, object> GetFromDbConverter(MemberInfo memberInfo, Type sourceType)
        {
            if(memberInfo.GetMemberInfoType() == typeof(DateTime) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return DateTime.Parse((string)obj);
                };
            }
            else if (memberInfo.GetMemberInfoType() == typeof(TimeSpan) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return TimeSpan.Parse((string)obj);
                };
            }
            else if (memberInfo.GetMemberInfoType() == typeof(char?) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return ((string)obj)[0];
                };
            }
            else if (Nullable.GetUnderlyingType(memberInfo.GetMemberInfoType()) == typeof(Guid) && sourceType == typeof(byte[]))
            {
                return (obj) =>
                {
                    return new Guid((byte[])obj);
                };
            }

            return null;
        }

        public Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
        {
            if (destType == typeof(DateTime) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return DateTime.Parse((string)obj);
                };
            }
            else if (destType == typeof(TimeSpan) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return TimeSpan.Parse((string)obj);
                };
            }
            else if (destType == typeof(char?) && sourceType == typeof(string))
            {
                return (obj) =>
                {
                    return ((string)obj)[0];
                };
            }
            else if (destType == typeof(Guid?) && sourceType == typeof(byte[]))
            {
                return (obj) =>
                {
                    return new Guid((byte[])obj);
                };
            }

            return null;
        }

        public Func<object, object> GetParameterConverter(DbCommand dbCommand, Type sourceType)
        {
            return null;
        }

        public Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
        {
            return null;
        }
    }
}
