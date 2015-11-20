using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NPoco
{
    public static class TypeHelpers
    {
        /// <summary>
        /// Gets an object's type even if it is null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="that">The object being extended.</param>
        /// <returns>The objects type.</returns>
        public static Type GetTheType<T>(this T that)
        {
            return typeof(T);
        }

        /// <summary>
        /// Gets an object's type even if it is null.
        /// </summary>
        /// <param name="that">The object being extended.</param>
        /// <returns>The objects type.</returns>
        public static Type GetTheType(this object that)
        {
            if (that != null)
            {
                return that.GetType();
            }

            return null;
        }
        
        public static bool IsAClass(this Type type)
        {
            return type != typeof(Type) && !type.GetTypeInfo().IsValueType && (type.GetTypeInfo().IsClass || type.GetTypeInfo().IsInterface) && type != typeof (string) && type != typeof(object) && !type.IsArray;
        }

#if NET40 || NET35
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
#endif
    }
}
