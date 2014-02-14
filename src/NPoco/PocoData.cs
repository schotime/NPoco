﻿using System;
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
     
        protected internal Type type;
        public string[] QueryColumns { get; protected set; }
        public string[] QueryColumnsWithAliases { get; protected set; }
        public TableInfo TableInfo { get; protected internal set; }
        public Dictionary<string, PocoColumn> Columns { get; protected internal set; }
        public Dictionary<string, PocoColumn> ColumnsByAlias { get; protected internal set; }
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

            // Work out bound properties
            Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            ColumnsByAlias = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (var mi in ReflectionUtils.GetFieldsAndPropertiesForClasses(t))
            {
                ColumnInfo ci = ColumnInfo.FromMemberInfo(mi);
                if (ci.IgnoreColumn)
                    continue;

                var pc = new PocoColumn
                {
                    MemberInfo = mi,
                    ColumnName = ci.ColumnName,
                    AliasName = ci.AliasName,
                    ResultColumn = ci.ResultColumn,
                    ForceToUtc = ci.ForceToUtc,
                    ColumnType = ci.ColumnType
                };

                if (Mapper != null && !Mapper.MapMemberToColumn(mi, ref pc.ColumnName, ref pc.ResultColumn))
                    continue;

                // Store it
                Columns.Add(pc.ColumnName, pc);

                // Store it by alias if set
                if (!string.IsNullOrWhiteSpace(pc.AliasName))
                {
                    ColumnsByAlias.Add(pc.AliasName, pc);
                }
            }

            // Build column list for automatic select
            QueryColumns = Columns.Where(c => !c.Value.ResultColumn).Select(c => c.Key).ToArray();

            // Same as QueryColumns, but will use `columnname as aliasname` if an alias exists for the column (ex. [Alias] attribute)
            QueryColumnsWithAliases = Columns.Where(c => !c.Value.ResultColumn).Select(kvp => ColumnNameWithAlias(kvp.Value)).ToArray();
        }

        protected string ColumnNameWithAlias(PocoColumn pc)
        {
            if (!string.IsNullOrWhiteSpace(pc.AliasName))
            {
                return string.Format("{0} as {1}", pc.ColumnName, pc.AliasName);
            }

            return pc.ColumnName;
        }
    }
}
