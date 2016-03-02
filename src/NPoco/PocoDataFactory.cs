using System;
using System.Collections.Generic;

namespace NPoco
{
    public class PocoDataFactory
    {
        private readonly MapperCollection _mapper;
        private readonly Cache<Type, PocoDataBuilder> _pocoDatas = Cache<Type, PocoDataBuilder>.CreateStaticCache();

        public PocoDataFactory(MapperCollection mapper)
        {
            _mapper = mapper;
        }

        public PocoDataFactory(Func<Type, PocoDataFactory, PocoDataBuilder> resolver)
        {
            Resolver = resolver;
        }

        public Func<Type, PocoDataFactory, PocoDataBuilder> Resolver { get; set; }

        public PocoData ForType(Type type)
        {
#if !NET35
            if (type == typeof(System.Dynamic.ExpandoObject) || type == typeof(PocoExpando))
                throw new InvalidOperationException("Can't use dynamic types with this method");
#endif
            var pocoDataBuilder = _pocoDatas.Get(type, (Resolver == null 
                ? new Func<PocoDataBuilder>(() => new PocoDataBuilder(type, _mapper, this).Init())
                : new Func<PocoDataBuilder>(() => Resolver(type, this))));
            return pocoDataBuilder.Build();
        }
        public PocoData ForObject(object o, string primaryKeyName, bool autoIncrement)
        {
            var t = o.GetType();
#if !NET35
            if (t == typeof(System.Dynamic.ExpandoObject) || t == typeof(PocoExpando))
            {
                var pd = new PocoData();
                pd.TableInfo = new TableInfo();
                pd.Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
                pd.Columns.Add(primaryKeyName, new ExpandoColumn() {ColumnName = primaryKeyName});
                pd.TableInfo.PrimaryKey = primaryKeyName;
                pd.TableInfo.AutoIncrement = autoIncrement;
                foreach (var col in ((IDictionary<string, object>) o))
                {
                    if (col.Key != primaryKeyName)
                        pd.Columns.Add(col.Key, new ExpandoColumn() {ColumnName = col.Key, MemberInfoData = new MemberInfoData(col.Key, col.Value.GetTheType() ?? typeof(object), typeof(object)) });
                }
                return pd;
            }
            else
#endif
                return ForType(t);
        }
    }
}