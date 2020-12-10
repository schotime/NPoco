using System;
using System.Collections.Generic;

namespace NPoco
{
    public interface IPocoDataFactory
    {
        PocoData ForType(Type type);
        TableInfo TableInfoForType(Type type);
        PocoData ForObject(object o, string primaryKeyName, bool autoIncrement);
    }

    public class FluentPocoDataFactory : IPocoDataFactory
    {
        private readonly MapperCollection _mapperCollection;
        private readonly Cache<Type, InitializedPocoDataBuilder> _pocoDatas = Cache<Type, InitializedPocoDataBuilder>.CreateStaticCache();
        public Func<Type, IPocoDataFactory, InitializedPocoDataBuilder> Resolver { get; private set; }

        public FluentPocoDataFactory(Func<Type, IPocoDataFactory, InitializedPocoDataBuilder> resolver, MapperCollection mapperCollection)
        {
            _mapperCollection = mapperCollection;
            Resolver = resolver;
        }
        
        public PocoData ForType(Type type)
        {
            PocoDataFactory.Guard(type);
            var pocoDataBuilder = _pocoDatas.Get(type, () => BaseClassFalbackPocoDataBuilder(type));
            return pocoDataBuilder.Build();
        }

        public TableInfo TableInfoForType(Type type)
        {
            PocoDataFactory.Guard(type);
            var pocoDataBuilder = _pocoDatas.Get(type, () => BaseClassFalbackPocoDataBuilder(type));
            return pocoDataBuilder.BuildTableInfo();
        }

        public PocoData ForObject(object o, string primaryKeyName, bool autoIncrement)
        {
            return PocoDataFactory.ForObjectStatic(o, primaryKeyName, autoIncrement, ForType, _mapperCollection);
        }

        private InitializedPocoDataBuilder BaseClassFalbackPocoDataBuilder(Type type)
        {
            var builder = Resolver(type, this);
            var persistedType = builder.BuildTableInfo().PersistedType;
            if (persistedType == null || persistedType == type)
            {
                return builder;
            }
            return Resolver(type, this);
        }
    }

    public class PocoDataFactory : IPocoDataFactory
    {
        private readonly static Cache<Type, InitializedPocoDataBuilder> _pocoDatas = Cache<Type, InitializedPocoDataBuilder>.CreateStaticCache();
        private readonly MapperCollection _mapper;

        public PocoDataFactory(MapperCollection mapper)
        {
            _mapper = mapper;
        }

        public PocoData ForType(Type type)
        {
            Guard(type);
            var pocoDataBuilder = _pocoDatas.Get(type, () => BaseClassFallbackPocoDataBuilder(type));
            return pocoDataBuilder.Build();
        }

        public TableInfo TableInfoForType(Type type)
        {
            Guard(type);
            var pocoDataBuilder = _pocoDatas.Get(type, () => BaseClassFallbackPocoDataBuilder(type));
            return pocoDataBuilder.BuildTableInfo();
        }

        public PocoData ForObject(object o, string primaryKeyName, bool autoIncrement)
        {
            return ForObjectStatic(o, primaryKeyName, autoIncrement, ForType, _mapper);
        }

        private InitializedPocoDataBuilder BaseClassFallbackPocoDataBuilder(Type type)
        {
            var builder = new PocoDataBuilder(type, _mapper).Init();
            var persistedType = builder.BuildTableInfo().PersistedType;
            if (persistedType == null || persistedType == type)
            {
                return builder;
            }
            return new PocoDataBuilder(persistedType, _mapper).Init();
        }

        public static PocoData ForObjectStatic(object o, string primaryKeyName, bool autoIncrement, Func<Type, PocoData> fallback, MapperCollection mapper)
        {
            var t = o.GetType();
            if (t == typeof (System.Dynamic.ExpandoObject) || t == typeof (PocoExpando))
            {
                var pd = new PocoData(t, mapper, Singleton<NullFastCreate>.Instance)
                {
                    TableInfo = new TableInfo
                    {
                        PrimaryKey = primaryKeyName,
                        AutoIncrement = autoIncrement
                    },
                    Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase)
                };
                foreach (var col in ((IDictionary<string, object>)o))
                {
                    pd.Columns.Add(col.Key, new ExpandoColumn
                    {
                        ColumnName = col.Key,
                        MemberInfoData = new MemberInfoData(col.Key, col.Value.GetTheType() ?? typeof(object), typeof(object)),
                    });
                }
                if (!pd.Columns.ContainsKey(primaryKeyName))
                {
                    pd.Columns.Add(primaryKeyName, new ExpandoColumn { ColumnName = primaryKeyName, ColumnType = typeof(object) });
                }
                return pd;
            }
            else
                return fallback(t);
        }

        public static void Guard(Type type)
        {
            if (type == typeof(System.Dynamic.ExpandoObject) || type == typeof(PocoExpando))
                throw new InvalidOperationException("Can't use dynamic types with this method");
        }

    }
}