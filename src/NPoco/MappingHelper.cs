using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NPoco.FastJSON;

namespace NPoco
{
    public class MappingHelper
    {
        static readonly EnumMapper EnumMapper = new EnumMapper();
        static readonly Cache<Type, Type> UnderlyingTypes = Cache<Type, Type>.CreateStaticCache();

        public static Func<object, object> GetConverter(MapperCollection mapper, PocoColumn pc, Type srcType, Type dstType)
        {
            Func<object, object> converter = null;

            // Get converter from the mapper
            if (mapper != null)
            {
                converter = pc != null ? mapper.Find(x => x.GetFromDbConverter(pc.MemberInfo, srcType)) : mapper.Find(x => x.GetFromDbConverter(dstType, srcType));
                if (converter != null)
                    return converter;
            }

            if (pc != null && pc.StoredAsJson)
            {
                converter = delegate(object src)
                {
                    return new JsonDeserializer(Database.JsonParameters, JSON.Manager).ToObject((string)src, dstType);
                };
                return converter;
            }

            // Standard DateTime->Utc mapper
            if (pc != null && pc.ForceToUtc && srcType == typeof(DateTime) && (dstType == typeof(DateTime) || dstType == typeof(DateTime?)))
            {
                converter = delegate(object src) { return new DateTime(((DateTime)src).Ticks, DateTimeKind.Utc); };
                return converter;
            }

            // Forced type conversion including integral types -> enum
            var underlyingType = UnderlyingTypes.Get(dstType, () => Nullable.GetUnderlyingType(dstType));
            if (dstType.IsEnum || (underlyingType != null && underlyingType.IsEnum))
            {
                if (srcType == typeof(string))
                {
                    converter = src => EnumMapper.EnumFromString((underlyingType ?? dstType), (string)src);
                    return converter;
                }

                if (IsIntegralType(srcType))
                {
                    converter = src => Enum.ToObject((underlyingType ?? dstType), src);
                    return converter;
                }
            }
            else if (!dstType.IsAssignableFrom(srcType))
            {
                converter = src => Convert.ChangeType(src, (underlyingType ?? dstType), null);
            }
            return converter;
        }

        static bool IsIntegralType(Type t)
        {
            var tc = Type.GetTypeCode(t);
            return tc >= TypeCode.SByte && tc <= TypeCode.UInt64;
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}
