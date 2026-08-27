using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace NPoco
{
    /// <summary>
    /// Caches reflection lookups (PropertyInfo, parsed enum values, compiled setters) used to talk to
    /// provider-specific ADO.NET types (e.g. NpgsqlParameter, SqlCeParameter, SqlGeography) without taking
    /// a compile-time dependency on those provider assemblies. Avoids repeating GetProperty/Enum.Parse/
    /// PropertyInfo.SetValue on every parameter bound, since these can run once per column per row.
    /// </summary>
    internal static class ReflectionCache
    {
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> Properties = new();
        private static readonly ConcurrentDictionary<(Type, string), object> EnumValues = new();
        private static readonly ConcurrentDictionary<(Type, string), Action<object, object>> Setters = new();

        public static PropertyInfo GetProperty(Type type, string name)
        {
            return Properties.GetOrAdd((type, name), key => key.Item1.GetProperty(key.Item2));
        }

        public static object GetEnumValue(Type enumType, string name)
        {
            return EnumValues.GetOrAdd((enumType, name), key => Enum.Parse(key.Item1, key.Item2));
        }

        /// <summary>
        /// Returns a cached delegate that sets the named property, compiled once via an Expression Tree
        /// instead of going through PropertyInfo.SetValue's reflection Invoke on every call. Null if the
        /// type has no such property (mirrors what PropertyInfo.SetValue would do: caller gets a
        /// NullReferenceException invoking a null delegate, same as calling SetValue on a null PropertyInfo).
        /// </summary>
        public static Action<object, object> GetSetter(Type type, string name)
        {
            return Setters.GetOrAdd((type, name), key =>
            {
                var property = GetProperty(key.Item1, key.Item2);
                if (property == null) return null;

                var targetParam = Expression.Parameter(typeof(object), "target");
                var valueParam = Expression.Parameter(typeof(object), "value");
                var typedTarget = Expression.Convert(targetParam, property.DeclaringType);
                var typedValue = Expression.Convert(valueParam, property.PropertyType);
                var assign = Expression.Assign(Expression.Property(typedTarget, property), typedValue);

                return Expression.Lambda<Action<object, object>>(assign, targetParam, valueParam).Compile();
            });
        }
    }
}
