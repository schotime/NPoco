using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;

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

        public Func<DbDataReader, object> CreateDelegate { get; set; }

        public object Create(DbDataReader dataReader)
        {
            try
            {
                return CreateDelegate(dataReader);
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

            var constructorInfo = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault(x => x.GetParameters().Length == 0);
            if (constructorInfo == null)
                return _ => null;

            // var poco=new T()
            var constructor = new DynamicMethod(Guid.NewGuid().ToString(), _type, new[] { typeof(DbDataReader) }, true);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);

            try
            {
                var del = constructor.CreateDelegate(Expression.GetFuncType(typeof(DbDataReader), typeof(object)));
                return del as Func<DbDataReader, object>;
            }
            catch (Exception exception)
            {
                throw new Exception("Error trying to create type " + _type, exception);
            }
        }
    }
}