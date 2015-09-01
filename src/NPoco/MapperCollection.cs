using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NPoco
{
    public class MapperCollection : List<IMapper>
    {
        public Dictionary<Type, ObjectFactoryDelegate> Factory = new Dictionary<Type, ObjectFactoryDelegate>();

        public delegate object ObjectFactoryDelegate(IDataReader dataReader);

        public Func<object, object> Find(Func<IMapper, Func<object, object>> predicate)
        {
            return this.Select(predicate).FirstOrDefault(x => x != null);
        }

        public object FindAndExecute(Func<IMapper, Func<object, object>> predicate, object value)
        {
            var converter = Find(predicate);
            return converter != null ? converter(value) : value;
        }
    }
}