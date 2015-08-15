using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco
{
    public class FastCreate
    {
        private readonly Type _type;

        public FastCreate(Type type)
        {
            _type = type;
            CreateDelegate = GetCreateDelegate();
        }

        public Func<object> CreateDelegate { get; set; }

        public object Create()
        {
            return CreateDelegate();
        }

        private Func<object> GetCreateDelegate()
        {
            var constructorInfo = _type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
            if (constructorInfo == null)
                return () => null;

            // var poco=new T()
            var constructor = new DynamicMethod(Guid.NewGuid().ToString(), _type, new Type[0], true);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);

            var del = constructor.CreateDelegate(Expression.GetFuncType(new[] {typeof (object)}));
            return del as Func<object>;
        }
    }
}