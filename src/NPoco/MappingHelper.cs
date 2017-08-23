using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

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
                converter = pc != null && pc.MemberInfoData != null ? mapper.Find(x => x.GetFromDbConverter(pc.MemberInfoData.MemberInfo, srcType)) : mapper.Find(x => x.GetFromDbConverter(dstType, srcType));
                if (converter != null)
                    return converter;
            }

            if (pc != null && pc.SerializedColumn)
            {
                converter = delegate(object src)
                {
                    return DatabaseFactory.ColumnSerializer.Deserialize((string) src, dstType);
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
            if (dstType.GetTypeInfo().IsEnum || (underlyingType != null && underlyingType.GetTypeInfo().IsEnum))
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
            else if ((!pc?.ValueObjectColumn ?? true) && !dstType.IsAssignableFrom(srcType))
            {
                converter = src => Convert.ChangeType(src, (underlyingType ?? dstType), null);
            }
            return converter;
        }

        static bool IsIntegralType(Type t)
        {
            //var tc = Type.GetTypeCode(t);
            //return tc >= TypeCode.SByte && tc <= TypeCode.UInt64;
            //Not available for now

            return new[]
                   {
                       typeof (SByte), typeof (Byte),
                       typeof (Int16), typeof (UInt16),
                       typeof (Int32), typeof (UInt32),
                       typeof (Int64), typeof (UInt64)
                   }.Contains(t);
        }

        public static object GetDefault(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}
