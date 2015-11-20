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
            if (type.IsValueType || type == typeof(string) || type == typeof(byte[]) || type == typeof(Dictionary<string, object>) || type.IsArray)
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
            if (member.MemberType == MemberTypes.Field)
                type = ((FieldInfo) member).FieldType;
            else if (member.MemberType == MemberTypes.Property)
                type = ((PropertyInfo) member).PropertyType;
            else if (member.MemberType == MemberTypes.Custom)
                type = ((DynamicMember) member).ReflectedType;
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
            return member.MemberType == MemberTypes.Field;
        }

        public static object GetMemberInfoValue(this MemberInfo member, object obj)
        {
            object val;
            if (member.MemberType == MemberTypes.Field)
                val = ((FieldInfo)member).GetValue(obj);
            else if(member.MemberType == MemberTypes.Property)
                val = ((PropertyInfo)member).GetValue(obj, null);
            else if (member.MemberType == MemberTypes.Custom)
                val = ((DynamicMember)member).GetValue(obj);
            else
                throw new NotSupportedException();
            return val;
        }

        public static void SetMemberInfoValue(this MemberInfo member, object obj, object value)
        {
            if (member.MemberType == MemberTypes.Field)
                ((FieldInfo)member).SetValue(obj, value);
            else
                ((PropertyInfo)member).SetValue(obj, value, null);
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
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericTypeDefinition)
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
                if (type.IsGenericType)
                    return type;

                type = type.BaseType;
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
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
                type = type.BaseType;
            }

            foreach (var i in instanceType.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}