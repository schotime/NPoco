using System;
using System.Linq;

namespace NPoco
{
    public class TableInfo
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
        public bool AutoIncrement { get; set; }
        public string SequenceName { get; set; }

        public static TableInfo FromPoco(Type t)
        {
            var tableInfo = new TableInfo();

            // Get the table name
            var a = t.GetCustomAttributes(typeof(TableNameAttribute), true);
            tableInfo.TableName = a.Length == 0 ? t.Name : (a[0] as TableNameAttribute).Value;

            // Get the primary key
            a = t.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            tableInfo.PrimaryKey = a.Length == 0 ? "ID" : (a[0] as PrimaryKeyAttribute).Value;
            tableInfo.SequenceName = a.Length == 0 ? null : (a[0] as PrimaryKeyAttribute).SequenceName;
            tableInfo.AutoIncrement = a.Length == 0 ? true : (a[0] as PrimaryKeyAttribute).AutoIncrement;

            // Set autoincrement false if primary key has multiple columns
            tableInfo.AutoIncrement = tableInfo.AutoIncrement ? !tableInfo.PrimaryKey.Contains(',') : tableInfo.AutoIncrement;

            return tableInfo;
        }
    }
}