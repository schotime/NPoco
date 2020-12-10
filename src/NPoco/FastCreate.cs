using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace NPoco
{
    public interface IFastCreate
    {
        object Create(DbDataReader dataReader);
    }

    public class FastCreate : IFastCreate
    {
        private readonly Type _type;
        private readonly MapperCollection _mapperCollection;
        private ConstructorInfo _constructorInfo;
        private Func<DbDataReader, object> _createDelegate;

        public FastCreate(Type type, MapperCollection mapperCollection)
        {
            _type = type;
            _mapperCollection = mapperCollection;
        }

        public object Create(DbDataReader dataReader)
        {
            try
            {
                _constructorInfo ??= GetConstructorInfo(_type);
                _createDelegate ??= GetCreateDelegate();
                return _createDelegate(dataReader);
            }
            catch (Exception exception)
            {
                throw new Exception("Error trying to create type " + _type, exception);
            }
        }

        private Func<DbDataReader, object> GetCreateDelegate()
        {
            if (_mapperCollection.HasFactory(_type))
                return dataReader => _mapperCollection.GetFactory(_type)(dataReader);

            if (_constructorInfo == null)
                return _ => null;

            var create = CreateObjectFactoryMethodWithCtorParams(_constructorInfo);
            var parameters = _constructorInfo.GetParameters()
                .Select(x => MappingHelper.GetDefault(x.ParameterType))
                .ToArray();
            return x => create(parameters);
        }

        private static Func<object[], object> CreateObjectFactoryMethodWithCtorParams(ConstructorInfo ctor)
        {
            var dm = new DynamicMethod(string.Format("_ObjectFactory_{0}", Guid.NewGuid()), typeof(object), new Type[] { typeof(object[]) }, true);
            var il = dm.GetILGenerator();

            var parameters = ctor.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0); // [args]
                EmitInt32(il, i); // [args][index]
                il.Emit(OpCodes.Ldelem_Ref); // [item-in-args-at-index]

                var paramType = parameters[i].ParameterType;
                if (paramType != typeof(object))
                {
                    il.Emit(OpCodes.Unbox_Any, paramType); // same as a cast if ref-type
                }
            }
            il.Emit(OpCodes.Newobj, ctor); // [new-object]
            il.Emit(OpCodes.Ret); // nothing
            return (Func<object[], object>)dm.CreateDelegate(typeof(Func<object[], object>));
        }

        private static void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            var constructorParameters = type
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(x => new
                {
                    Constructor = x, 
                    Parameters = x.GetParameters()
                })
                .OrderByDescending(x => x.Parameters.Length)
                .ToList();

            var attributeConstructor = constructorParameters.FirstOrDefault(x => x.Constructor.GetCustomAttribute(typeof(ConstructAttribute)) != null);
            if (attributeConstructor != null)
            {
                return attributeConstructor.Constructor;
            }

            var getParameterLess = constructorParameters.SingleOrDefault(x => x.Parameters.Length == 0);

            return getParameterLess != null
                ? getParameterLess.Constructor
                : constructorParameters.FirstOrDefault()?.Constructor;
        }
    }
}