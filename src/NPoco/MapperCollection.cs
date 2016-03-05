using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

namespace NPoco
{
    public class MapperCollection : List<IMapper>
    {
        internal readonly Dictionary<Type, ObjectFactoryDelegate> Factories = new Dictionary<Type, ObjectFactoryDelegate>();
        public delegate object ObjectFactoryDelegate(DbDataReader dataReader);

        public MapperCollection()
        {
#if !NET35
            Factories.Add(typeof(object), x => new PocoExpando());
#endif
            Factories.Add(typeof(IDictionary<string, object>), x => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            Factories.Add(typeof(Dictionary<string, object>), x => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        }

        public void RegisterFactory<T>(Func<DbDataReader, T> factory)
        {
            Factories[typeof(T)] = x => factory(x);
        }

        public ObjectFactoryDelegate GetFactory(Type type)
        {
            return Factories.ContainsKey(type) ? Factories[type] : null;
        }

        public bool HasFactory(Type type)
        {
            return Factories.ContainsKey(type);
        }

        public void ClearFactories(Type type = null)
        {
            if (type != null)
            {
                Factories.Remove(type);
            }
            else
            {
                Factories.Clear();
            }
        }

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