using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NPoco
{
    public class TableInfo
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
        public bool AutoIncrement { get; set; }
        public string SequenceName { get; set; }
        public string AutoAlias { get; set; }
        public bool UseOutputClause { get; set; }
        public Type PersistedType { get; set; }
        public List<IAlterStatementHook> AlterStatementHooks { get; set; } = new();

        public TableInfo Clone()
        {
            return new TableInfo
            {
                AutoAlias = AutoAlias,
                AutoIncrement = AutoIncrement,
                TableName = TableName,
                PrimaryKey = PrimaryKey,
                SequenceName = SequenceName,
                UseOutputClause = UseOutputClause,
                PersistedType = PersistedType,
				AlterStatementHooks = AlterStatementHooks,
            };
        }
    }
}