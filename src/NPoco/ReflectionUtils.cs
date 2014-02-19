﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            if (member is FieldInfo)
                type = ((FieldInfo) member).FieldType;
            else
                type = ((PropertyInfo) member).PropertyType;
            return type;
        }

        public static bool IsField(this MemberInfo member)
        {
            return member is FieldInfo;
        }

        public static object GetMemberInfoValue(this MemberInfo member, object obj)
        {
            object val;
            if (member is FieldInfo)
                val = ((FieldInfo)member).GetValue(obj);
            else
                val = ((PropertyInfo)member).GetValue(obj, null);
            return val;
        }

        public static void SetMemberInfoValue(this MemberInfo member, object obj, object value)
        {
            if (member is FieldInfo)
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
    }
}