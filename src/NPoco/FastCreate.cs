using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace NPoco
{
    public class FastCreate
    {
        private readonly Type _type;
        private readonly MapperCollection _mapperCollection;

        public FastCreate(Type type, MapperCollection mapperCollection)
        {
            _type = type;
            _mapperCollection = mapperCollection;
            CreateDelegate = GetCreateDelegate();
        }

        public Func<IDataReader, object> CreateDelegate { get; set; }

        public object Create(IDataReader dataReader)
        {
            return CreateDelegate(dataReader);
        }

        private Func<IDataReader, object> GetCreateDelegate()
        {
            if (_mapperCollection.Factory.ContainsKey(_type))
                return dataReader => _mapperCollection.Factory[_type](dataReader);

            if (_type.IsAbstract || _type.IsInterface)
                throw new Exception("Custom mapper needs to be registered in the MapperCollection factory");

            var constructorInfo = _type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
            if (constructorInfo == null)
                return _ => null;

            // var poco=new T()
            var constructor = new DynamicMethod(Guid.NewGuid().ToString(), _type, new[] { typeof(IDataReader) }, true);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);

            var del = constructor.CreateDelegate(Expression.GetFuncType(typeof (IDataReader), typeof (object)));
            return del as Func<IDataReader, object>;
        }
    }
}