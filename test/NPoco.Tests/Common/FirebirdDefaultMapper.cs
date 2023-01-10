using System;
using System.Reflection;
using NPoco;

namespace NPoco.Tests.Common
{
    internal class FirebirdDefaultMapper: DefaultMapper
    {
        private bool isNullable(Type type)
        {
            return (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>));
        }

        public override Func<object, object> GetFromDbConverter(Type DestType, Type SourceType)
        {
            // Db:String -> Guid
            if ((DestType == typeof (Guid)) && (SourceType == typeof (string)))
            {
                return src => Guid.Parse((string)src);
            }

            // Db:String -> Guid?
            if (isNullable(DestType))
            {
                var underlyingType = Nullable.GetUnderlyingType(DestType);
                if (underlyingType == typeof (Guid) )
                {
                    return src => (src == null ? (Guid?) null : Guid.Parse((string) src));
                }
            }

            return base.GetFromDbConverter(DestType, SourceType);
        }
    }

    internal class SqlTestDefaultMapper : DefaultMapper
    {
        public override Func<object, object> GetFromDbConverter(Type DestType, Type SourceType)
        {
            if ((DestType == typeof(StringObject)) && (SourceType == typeof(string)))
            {
                return src => new StringObject { MyValue = src?.ToString() };
            }

            if ((DestType == typeof(bool)) && (SourceType == typeof(string)))
            {
                return src => !string.IsNullOrEmpty(src.ToString()) && src.ToString()[0] == 'Y';
            }

            return base.GetFromDbConverter(DestType, SourceType);
        }

        public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
        {
            if ((sourceMemberInfo.GetMemberInfoType() == typeof(StringObject)) && (destType == typeof(string)))
            {
                return src => ((StringObject)(src))?.ToString();
            }

            if (sourceMemberInfo?.Name == "YorNBoolean")
            {
                return src => (bool)src ? "Y" : "N";
            }

            return base.GetToDbConverter(destType, sourceMemberInfo);
        }
    }
}
