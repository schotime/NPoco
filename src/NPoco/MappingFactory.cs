using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class MappingFactory
    {
        static readonly EnumMapper EnumMapper = new EnumMapper();
        static readonly Cache<Type, Type> UnderlyingTypes = Cache<Type, Type>.CreateStaticCache();

        public static Func<object, object> GetConverter(IMapper mapper, PocoColumn pc, Type srcType, Type dstType)
        {
            Func<object, object> converter = null;

            // Get converter from the mapper
            if (mapper != null)
            {
                converter = pc != null ? mapper.GetFromDbConverter(pc.MemberInfo, srcType) : mapper.GetFromDbConverter(dstType, srcType);
                if (converter != null)
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

        static T RecurseInheritedTypes<T>(Type t, Func<Type, T> cb)
        {
            while (t != null)
            {
                T info = cb(t);
                if (info != null)
                    return info;
                t = t.BaseType;
            }
            return default(T);
        }

        public static bool TryGetColumnByName(PocoData pocoData, string name, out PocoColumn pc, out PocoMember pm)
        {
            var columns = pocoData.Columns;
            var members = pocoData.Members;

            var member = members.FirstOrDefault(x => x.Name == name);
            if (member != null)
            {
                pm = member;
                pc = null;
                return true;
            }

            // Try to get the column by name directly (works when the poco property name matches the DB column name).
            var found = (columns.TryGetValue(name, out pc) || columns.TryGetValue(name.Replace("_", ""), out pc));
            if (!found)
            {
                // Try to get the column by the poco member name (the poco property name is different from the DB column name).
                pc = columns.Values.Where(c => c.MemberInfo.Name == name).FirstOrDefault();
                found = (pc != null);
            }
            if (!found)
            {
                var pcAlias = columns.Values.SingleOrDefault(x => string.Equals(x.ColumnAlias, name, StringComparison.OrdinalIgnoreCase));
                pc = pcAlias;
                found = (pcAlias != null);
            }

            if (found)
            {
                if (!pc.MemberInfo.IsField() && ((PropertyInfo)pc.MemberInfo).GetSetMethodOnDeclaringType() == null)
                {
                    found = false;
                }
            }

            pm = null;

            return found;
        }
    }
}
