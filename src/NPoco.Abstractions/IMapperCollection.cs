using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace NPoco
{
    public interface IMapperCollection : IList<IMapper>
    {
        public delegate object ObjectFactoryDelegate(DbDataReader dataReader);

        IColumnSerializer ColumnSerializer { get; set; }

        void ClearFactories(Type type = null);
        Func<object, object> Find(Func<IMapper, Func<object, object>> predicate);
        object FindAndExecute(Func<IMapper, Func<object, object>> predicate, object value);
        Func<object, object> FindFromDbConverter(MemberInfo destInfo, Type srcType);
        Func<object, object> FindFromDbConverter(Type destType, Type srcType);
        Func<object, object> FindToDbConverter(Type destType, MemberInfo srcInfo);
        ObjectFactoryDelegate GetFactory(Type type);
        bool HasFactory(Type type);
        void RegisterFactory<T>(Func<DbDataReader, T> factory);
    }
}