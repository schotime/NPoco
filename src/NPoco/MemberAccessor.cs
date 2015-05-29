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
        private readonly string _memberName;
        private Type _memberType;
        private Hashtable _mTypeHash;
        private bool _canRead;
        private readonly bool _canWrite;
        private readonly MemberInfo _member;

        /// <summary>
        /// Creates a new property accessor.
        /// </summary>
        /// <param name="targetType">Target object type.</param>
        /// <param name="memberName">Property name.</param>
        public MemberAccessor(Type targetType, string memberName)
        {
            _targetType = targetType;
            _memberName = memberName;
            MemberInfo memberInfo = ReflectionUtils.GetFieldsAndPropertiesForClasses(targetType).First(x => x.Name == memberName);
            //
            // Make sure the property exists
            //
            if (memberInfo == null)
            {
                throw new Exception(string.Format("Property \"{0}\" does not exist for type " + "{1}.", memberName, targetType));
            }

            _canRead = memberInfo.IsField() || ((PropertyInfo) memberInfo).CanRead;
            _canWrite = memberInfo.IsField() || ((PropertyInfo) memberInfo).CanWrite;
            _memberType = memberInfo.GetMemberInfoType();
            _member = memberInfo;

            InitTypes();

            if (_canWrite)
            {
                SetDelegate = GetSetDelegate();
            }
            //else
            //{
            //    throw new Exception(string.Format("Property \"{0}\" does" + " not have a set method.", _memberName));
            //}

            if (_canRead)
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
            if (SetDelegate != null)
            {
                SetDelegate(target, value);
            }
        }

        public object Get(object target)
        {
            if (GetDelegate != null)
            {
                return GetDelegate(target);
            }
            return null;
        }

        /// <summary>
        /// Thanks to Ben Ratzlaff for this snippet of code
        /// http://www.codeproject.com/cs/miscctrl/CustomPropGrid.asp
        /// 
        /// "Initialize a private hashtable with type-opCode pairs 
        /// so i dont have to write a long if/else statement when outputting msil"
        /// </summary>
        private void InitTypes()
        {
            _mTypeHash = new Hashtable();
            _mTypeHash[typeof(sbyte)] = OpCodes.Ldind_I1;
            _mTypeHash[typeof(byte)] = OpCodes.Ldind_U1;
            _mTypeHash[typeof(char)] = OpCodes.Ldind_U2;
            _mTypeHash[typeof(short)] = OpCodes.Ldind_I2;
            _mTypeHash[typeof(ushort)] = OpCodes.Ldind_U2;
            _mTypeHash[typeof(int)] = OpCodes.Ldind_I4;
            _mTypeHash[typeof(uint)] = OpCodes.Ldind_U4;
            _mTypeHash[typeof(long)] = OpCodes.Ldind_I8;
            _mTypeHash[typeof(ulong)] = OpCodes.Ldind_I8;
            _mTypeHash[typeof(bool)] = OpCodes.Ldind_I1;
            _mTypeHash[typeof(double)] = OpCodes.Ldind_R8;
            _mTypeHash[typeof(float)] = OpCodes.Ldind_R4;
        }

        private Action<object, object> GetSetDelegate()
        {
            Type[] setParamTypes = new Type[] { typeof(object), typeof(object) };
            Type setReturnType = null;

            var setMethod = new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, true);
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
            if (paramType.IsValueType)
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
            Type[] setParamTypes = new[] { typeof(object) };
            Type setReturnType = typeof (object);

            var getMethod = new DynamicMethod(Guid.NewGuid().ToString(), setReturnType, setParamTypes, true);
            // From the method, get an ILGenerator. This is used to
            // emit the IL that we want.
            //
            ILGenerator getIL = getMethod.GetILGenerator();
            
            getIL.DeclareLocal(typeof(object));
            getIL.Emit(OpCodes.Ldarg_0); //Load the first argument
            //(target object)
            //Cast to the source type
            getIL.Emit(OpCodes.Castclass, this._targetType);
            //Get the property value

            if (_member.IsField())
            {
                getIL.Emit(OpCodes.Ldfld, (FieldInfo)_member);
            }
            else
            {
                var targetGetMethod = ((PropertyInfo) _member).GetGetMethod();
                getIL.Emit(OpCodes.Callvirt, targetGetMethod);
                if (targetGetMethod.ReturnType.IsValueType)
                {
                    getIL.Emit(OpCodes.Box, targetGetMethod.ReturnType);
                    //Box if necessary
                }
            }

            getIL.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamTypes.Concat(new[]{setReturnType}).ToArray()));
            return del as Func<object, object>;
        }
    }
}