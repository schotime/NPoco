using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco
{
    /// <summary>
    /// The PropertyAccessor class provides fast dynamic access
    /// to a property of a specified target class.
    /// </summary>
    public class MemberAccessor
    {
        private readonly Type _targetType;
        private readonly Type _memberType;
        private readonly MemberInfo _member;

        private static readonly Hashtable _mTypeHash = new Hashtable
        {
            [typeof(sbyte)] = OpCodes.Ldind_I1,
            [typeof(byte)] = OpCodes.Ldind_U1,
            [typeof(char)] = OpCodes.Ldind_U2,
            [typeof(short)] = OpCodes.Ldind_I2,
            [typeof(ushort)] = OpCodes.Ldind_U2,
            [typeof(int)] = OpCodes.Ldind_I4,
            [typeof(uint)] = OpCodes.Ldind_U4,
            [typeof(long)] = OpCodes.Ldind_I8,
            [typeof(ulong)] = OpCodes.Ldind_I8,
            [typeof(bool)] = OpCodes.Ldind_I1,
            [typeof(double)] = OpCodes.Ldind_R8,
            [typeof(float)] = OpCodes.Ldind_R4
        };

        /// <summary>
        /// Creates a new property accessor.
        /// </summary>
        /// <param name="targetType">Target object type.</param>
        /// <param name="memberName">Property name.</param>
        public MemberAccessor(Type targetType, string memberName)
        {
            _targetType = targetType;
            MemberInfo memberInfo = ReflectionUtils.GetFieldsAndPropertiesForClasses(targetType).First(x => x.Name == memberName);

            if (memberInfo == null)
            {
                throw new Exception(string.Format("Property \"{0}\" does not exist for type " + "{1}.", memberName, targetType));
            }

            var canRead = memberInfo.IsField() || ((PropertyInfo) memberInfo).CanRead;
            var canWrite = memberInfo.IsField() || ((PropertyInfo) memberInfo).CanWrite;

            // roslyn automatically implemented properties, in particular for get-only properties: <{Name}>k__BackingField;
            if (!canWrite)
            {
                var backingFieldName = $"<{memberName}>k__BackingField";
                var backingFieldMemberInfo = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.Name == backingFieldName);
                if (backingFieldMemberInfo != null)
                {
                    memberInfo = backingFieldMemberInfo;
                    canWrite = true;
                }
            }

            _memberType = memberInfo.GetMemberInfoType();
            _member = memberInfo;

            if (canWrite)
            {
                SetDelegate = GetSetDelegate();
            }

            if (canRead)
            {
                GetDelegate = GetGetDelegate();
            }
        }

        private Func<object, object> GetDelegate = null;

        private Action<object, object> SetDelegate = null;

        /// <summary>
        /// Sets the property for the specified target.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <param name="value">Value to set.</param>
        public void Set(object target, object value)
        {
            SetDelegate?.Invoke(target, value);
        }

        public object Get(object target)
        {
            return GetDelegate?.Invoke(target);
        }

        private Action<object, object> GetSetDelegate()
        {
            Type[] setParamTypes = new Type[] { typeof(object), typeof(object) };
            Type setReturnType = null;

            var owner = _targetType.GetTypeInfo().IsAbstract || _targetType.GetTypeInfo().IsInterface ? null : _targetType;
            var setMethod = owner != null 
                ? new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, owner, true)
                : new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, true);
            // From the method, get an ILGenerator. This is used to
            // emit the IL that we want.
            //
            ILGenerator setIL = setMethod.GetILGenerator();
            //
            // Emit the IL. 
            //

            Type paramType = _memberType;
            setIL.Emit(OpCodes.Ldarg_0); //Load the first argument 
            //(target object)
            //Cast to the source type
            setIL.Emit(OpCodes.Castclass, this._targetType);
            setIL.Emit(OpCodes.Ldarg_1); //Load the second argument 
            //(value object)
            if (paramType.GetTypeInfo().IsValueType)
            {
                setIL.Emit(OpCodes.Unbox, paramType); //Unbox it 
                if (_mTypeHash[paramType] != null) //and load
                {
                    OpCode load = (OpCode)_mTypeHash[paramType];
                    setIL.Emit(load);
                }
                else
                {
                    setIL.Emit(OpCodes.Ldobj, paramType);
                }
            }
            else
            {
                setIL.Emit(OpCodes.Castclass, paramType); //Cast class
            }

            if (_member.IsField())
            {
                setIL.Emit(OpCodes.Stfld, (FieldInfo)_member);
            }
            else
            {
                MethodInfo targetSetMethod = ((PropertyInfo)this._member).GetSetMethodOnDeclaringType();
                if (targetSetMethod != null)
                {
                    setIL.Emit(OpCodes.Callvirt, targetSetMethod);
                }
                else
                {
                    setIL.ThrowException(typeof(MissingMethodException));
                }
            }
            setIL.Emit(OpCodes.Ret);

            var del = setMethod.CreateDelegate(Expression.GetActionType(setParamTypes));
            return del as Action<object, object>;
        }

        private Func<object, object> GetGetDelegate()
        {
            Type setParamType = typeof(object);
            Type[] setParamTypes = { setParamType };
            Type setReturnType = typeof(object);

            Type owner = _targetType.GetTypeInfo().IsAbstract || _targetType.GetTypeInfo().IsInterface ? null : _targetType;
            var getMethod = owner != null
                ? new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, owner, true)
                : new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, true);

            // From the method, get an ILGenerator. This is used to
            // emit the IL that we want.
            ILGenerator getIL = getMethod.GetILGenerator();

            getIL.Emit(OpCodes.Ldarg_0); //Load the first argument (target object)
            getIL.Emit(_targetType.GetTypeInfo().IsValueType ? OpCodes.Unbox : OpCodes.Castclass, _targetType);

            Type returnType;
            if (_member.IsField())
            {
                getIL.Emit(OpCodes.Ldfld, (FieldInfo)_member);
                returnType = _memberType;
            }
            else
            {
                var targetGetMethod = ((PropertyInfo)_member).GetGetMethod();
                var opCode = _targetType.GetTypeInfo().IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                getIL.Emit(opCode, targetGetMethod);
                returnType = targetGetMethod.ReturnType;
            }

            if (returnType.GetTypeInfo().IsValueType)
            {
                getIL.Emit(OpCodes.Box, returnType);
            }

            getIL.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamType, setReturnType));
            return del as Func<object, object>;
        }
    }
}