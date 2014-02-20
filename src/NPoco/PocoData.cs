using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NPoco
{
    public class PocoData
    {
        protected internal IMapper Mapper;
        internal bool EmptyNestedObjectNull;
        protected static readonly ThreadSafeDictionary<string, Type> AliasToType = new ThreadSafeDictionary<string, Type>();
     
        protected internal Type type;
        public KeyValuePair<string, PocoColumn>[] QueryColumns { get; protected set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        private readonly MappingFactory _mappingFactory;

        public MappingFactory MappingFactory
        {
            get { return _mappingFactory; }
        }

        public PocoData()
        {
            _mappingFactory = new MappingFactory(this);
        }

        public PocoData(Type t, IMapper mapper) : this()
        {
            type = t;
            Mapper = mapper;
            TableInfo = TableInfo.FromPoco(t);

            // Call column mapper
            if (Mapper != null)
                Mapper.GetTableInfo(t, TableInfo);

            var alias = FindAlias(type.Name, type);
            TableInfo.AutoAlias = alias;
            var index = 0;
            
            // Work out bound properties
            Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(t))
            {
                ColumnInfo ci = ColumnInfo.FromMemberInfo(mi);
                if (ci.IgnoreColumn)
                    continue;

                var pc = new PocoColumn();
                pc.MemberInfo = mi;
                pc.ColumnName = ci.ColumnName;
                pc.ResultColumn = ci.ResultColumn;
                pc.ForceToUtc = ci.ForceToUtc;
                pc.ColumnType = ci.ColumnType;

                if (Mapper != null && !Mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;
                
                pc.AutoAlias = alias + "_" + index++;

                // Store it
                Columns.Add(pc.ColumnName, pc);
            }

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn).ToArray();
        }

        protected string FindAlias(string typeName, Type type)
        {
            string alias;
            int i = 0;
            bool result = false;
            do
            {
                alias = typeName.CreateAlias() + (i==0 ? string.Empty : i.ToString());
                i++;
                if (AliasToType.ContainsKey(alias))
                    continue;
                AliasToType.Add(alias, type);
                result = true;
            } while (result == false);

            return alias;
        }
    }
}
