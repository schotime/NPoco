using System;
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

        public TableInfo Clone()
        {
            return new TableInfo()
            {
                AutoAlias = AutoAlias,
                AutoIncrement = AutoIncrement,
                TableName = TableName,
                PrimaryKey = PrimaryKey,
                SequenceName = SequenceName,
                UseOutputClause = UseOutputClause,
                PersistedType = PersistedType
            };
        }

        public static TableInfo FromPoco(Type t)
        {
            var tableInfo = new TableInfo();

            // Get the table name
            var a = t.GetTypeInfo().GetCustomAttributes(typeof(TableNameAttribute), true).ToArray();
            tableInfo.TableName = a.Length == 0 ? t.Name : (a[0] as TableNameAttribute).Value;

            // Get the primary key
            a = t.GetTypeInfo().GetCustomAttributes(typeof(PrimaryKeyAttribute), true).ToArray();
            tableInfo.PrimaryKey = a.Length == 0 ? "ID" : (a[0] as PrimaryKeyAttribute).Value;
            tableInfo.SequenceName = a.Length == 0 ? null : (a[0] as PrimaryKeyAttribute).SequenceName;
            tableInfo.AutoIncrement = a.Length == 0 ? true : (a[0] as PrimaryKeyAttribute).AutoIncrement;
            tableInfo.UseOutputClause = a.Length == 0 ? false : (a[0] as PrimaryKeyAttribute).UseOutputClause;

            // Set autoincrement false if primary key has multiple columns
            tableInfo.AutoIncrement = tableInfo.AutoIncrement ? !tableInfo.PrimaryKey.Contains(',') : tableInfo.AutoIncrement;

            a = t.GetTypeInfo().GetCustomAttributes(typeof(PersistedTypeAttribute), true).ToArray();
            tableInfo.PersistedType = a.Length == 0 ? null : (a[0] as PersistedTypeAttribute).PersistedType;

            return tableInfo;
        }
    }
}