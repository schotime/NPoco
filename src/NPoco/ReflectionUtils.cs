using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NPoco.RowMappers;

namespace NPoco
{
    public static class ReflectionUtils
    {
        public static List<MemberInfo> GetFieldsAndProperties<T>(BindingFlags bindingAttr)
        {
            return GetFieldsAndProperties(typeof(T), bindingAttr);
        }

        public static List<MemberInfo> GetFieldsAndPropertiesForClasses(Type type)
        {
            if (type.GetTypeInfo().IsValueType || type == typeof(string) || type == typeof(byte[]) || type == typeof(Dictionary<string, object>) || type.IsArray)
                return new List<MemberInfo>();

            return GetFieldsAndProperties(type);
        }

        public static List<MemberInfo> GetFieldsAndProperties(Type type)
        {
            return GetFieldsAndProperties(type, BindingFlags.Instance | BindingFlags.Public);
        }

        public static List<MemberInfo> GetFieldsAndProperties(Type type, BindingFlags bindingAttr)
        {
            List<MemberInfo> targetMembers = new List<MemberInfo>();

            targetMembers.AddRange(type.GetFields(bindingAttr).Where(x=>!x.IsInitOnly).ToArray());
            targetMembers.AddRange(type.GetProperties(bindingAttr));

            return targetMembers;
        }

        public static Type GetMemberInfoType(this MemberInfo member)
        {
            Type type;
            if (member is FieldInfo)
                type = ((FieldInfo) member).FieldType;
            else if (member is PropertyInfo)
                type = ((PropertyInfo) member).PropertyType;
            else if (member == null)
                type = typeof (object);
            else
                throw new NotSupportedException();

            return type;
        }

        public static bool IsDynamic(this MemberInfo member)
        {
#if !NET35
            return member.GetCustomAttributes(typeof(DynamicAttribute), true).Any();
#else
            return false;
#endif
        }


        public static bool IsField(this MemberInfo member)
        {
            return member is FieldInfo;
        }

        public static MethodInfo GetSetMethodOnDeclaringType(this PropertyInfo propertyInfo)
        {
            var methodInfo = propertyInfo.GetSetMethod(true);
            return methodInfo ?? propertyInfo
                                    .DeclaringType
                                    .GetProperty(propertyInfo.Name)
                                    .GetSetMethod(true);
        }

        public static bool IsOrHasGenericInterfaceTypeOf(this Type type, Type genericTypeDefinition)
        {
            return type.GetTypeWithGenericTypeDefinitionOf(genericTypeDefinition) != null;
        }

        public static Type GetTypeWithGenericTypeDefinitionOf(this Type type, Type genericTypeDefinition)
        {
            foreach (var t in type.GetInterfaces())
            {
                if (t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    return t;
                }
            }

            var genericType = type.GetGenericType();
            if (genericType != null && genericType.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return genericType;
            }

            return null;
        }

        public static Type GetGenericType(this Type type)
        {
            while (type != null)
            {
                if (type.GetTypeInfo().IsGenericType)
                    return type;

                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }

        public static Type GetTypeWithInterfaceOf(this Type type, Type interfaceType)
        {
            if (type == interfaceType) return interfaceType;

            foreach (var t in type.GetInterfaces())
            {
                if (t == interfaceType)
                    return t;
            }

            return null;
        }

        public static bool IsOfGenericType(this Type instanceType, Type genericType)
        {
            Type type = instanceType;
            while (type != null)
            {
                if (type.GetTypeInfo().IsGenericType &&
                    type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
                type = type.GetTypeInfo().BaseType;
            }

            foreach (var i in instanceType.GetInterfaces())
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<Attribute> GetCustomAttributes(MemberInfo memberInfo)
        {
#if NET35 || NET40
            var attrs = Attribute.GetCustomAttributes(memberInfo);
#else
            var attrs = memberInfo.GetCustomAttributes();
#endif

            return attrs;
        }

        public static IEnumerable<Attribute> GetCustomAttributes(MemberInfo memberInfo, Type type)
        {
#if NET35 || NET40
            var attrs = Attribute.GetCustomAttributes(memberInfo, type);
#else
            var attrs = memberInfo.GetCustomAttributes(type);
#endif

            return attrs;
        }
    }
}