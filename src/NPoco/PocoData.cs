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
        protected Cache<string, Type> AliasToType;
     
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

        public PocoData(Type t, IMapper mapper, Cache<string, Type> aliasToTypeCache)
        {
            _mappingFactory = new MappingFactory(this);
            AliasToType = aliasToTypeCache;
            type = t;
            Mapper = mapper;
            TableInfo = TableInfo.FromPoco(t);
            
            // Call column mapper
            if (Mapper != null)
                Mapper.GetTableInfo(t, TableInfo);

            var alias = CreateAlias(type.Name, type);
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
                pc.TableInfo = TableInfo;
                pc.MemberInfo = mi;
                pc.ColumnName = ci.ColumnName;
                pc.ResultColumn = ci.ResultColumn;
                pc.ForceToUtc = ci.ForceToUtc;
                pc.ComputedColumn = ci.ComputedColumn;
                pc.ColumnType = ci.ColumnType;
                pc.ColumnAlias = ci.ColumnAlias;
                pc.VersionColumn = ci.VersionColumn;
                pc.VersionColumnType = ci.VersionColumnType;

                if (Mapper != null && !Mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;
                
                pc.AutoAlias = alias + "_" + index++;

                // Store it
                if (!Columns.ContainsKey(pc.ColumnName))
                    Columns.Add(pc.ColumnName, pc);
            }

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn).ToArray();
        }

        protected string CreateAlias(string typeName, Type typeIn)
        {
            string alias;
            int i = 0;
            bool result = false;
            string name = string.Join(string.Empty, typeName.BreakUpCamelCase().Split(' ').Select(x => x.Substring(0, 1)).ToArray());
            do
            {
                alias = name + (i == 0 ? string.Empty : i.ToString());
                i++;

                if (AliasToType.AddIfNotExists(alias, typeIn))
                {
                    continue;
                }

                result = true;
            } while (result == false);

            return alias;
        }
    }
}
