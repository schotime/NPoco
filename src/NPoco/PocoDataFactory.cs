using System;
using System.Collections.Generic;

namespace NPoco
{
    public class PocoDataFactory
    {
        private readonly IMapper _mapper;
        private Cache<Type, PocoData> _pocoDatas = new Cache<Type, PocoData>();

        public PocoDataFactory(IMapper mapper)
        {
            _mapper = mapper;
        }

        public PocoDataFactory(Func<Type, PocoData> resolver)
        {
            Resolver = resolver;
        }

        public Func<Type, PocoData> Resolver { get; set; }
        public PocoData ForType(Type type)
        {
            return ForType(type, false);
        }
        public PocoData ForType(Type type, bool emptyNestedObjectNull)
        {
#if !POCO_NO_DYNAMIC
            if (type == typeof(System.Dynamic.ExpandoObject) || type == typeof(PocoExpando))
                throw new InvalidOperationException("Can't use dynamic types with this method");
#endif
            Func<PocoData> pocoDataFunc = (Resolver == null 
                ? new Func<PocoData>(() => new PocoData(type, _mapper)) 
                : new Func<PocoData>(() => Resolver(type)));
            var pocoData = _pocoDatas.Get(type, pocoDataFunc);
            pocoData.EmptyNestedObjectNull = emptyNestedObjectNull;
            return pocoData;
        }
        public PocoData ForObject(object o, string primaryKeyName)
        {
            var t = o.GetType();
#if !POCO_NO_DYNAMIC
            if (t == typeof(System.Dynamic.ExpandoObject) || t == typeof(PocoExpando))
            {
                var pd = new PocoData();
                pd.TableInfo = new TableInfo();
                pd.Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
                pd.Columns.Add(primaryKeyName, new ExpandoColumn() {ColumnName = primaryKeyName});
                pd.TableInfo.PrimaryKey = primaryKeyName;
                pd.TableInfo.AutoIncrement = true;
                foreach (var col in ((IDictionary<string, object>) o).Keys)
                {
                    if (col != primaryKeyName)
                        pd.Columns.Add(col, new ExpandoColumn() {ColumnName = col});
                }
                return pd;
            }
            else
#endif
                return ForType(t);
        }
    }
}