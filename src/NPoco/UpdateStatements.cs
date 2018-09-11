using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NPoco
{
    public partial class Database
    {
        public class UpdateStatements
        {
            internal static PreparedUpdateStatement PrepareUpdate(Database database, PocoData pd, string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
            {
                var sb = new StringBuilder();
                var index = 0;
                var rawvalues = new List<object>();
                string versionName = null;
                object versionValue = null;
                var versionColumnType = VersionColumnType.Number;

                var primaryKeyValuePairs = GetPrimaryKeyValues(database, pd, primaryKeyName, primaryKeyValue ?? poco, primaryKeyValue == null);

                foreach (var pocoColumn in pd.Columns.Values)
                {
                    // Don't update the primary key, but grab the value if we don't have it
                    if (primaryKeyValuePairs.ContainsKey(pocoColumn.ColumnName))
                    {
                        if (primaryKeyValue == null)
                            primaryKeyValuePairs[pocoColumn.ColumnName] = database.ProcessMapper(pocoColumn, pocoColumn.GetValue(poco));
                        continue;
                    }

                    // Dont update result only columns
                    if (pocoColumn.ResultColumn
                        || (pocoColumn.ComputedColumn && (pocoColumn.ComputedColumnType == ComputedColumnType.Always || pocoColumn.ComputedColumnType == ComputedColumnType.ComputedOnUpdate)))
                    {
                        continue;
                    }

                    if (!pocoColumn.VersionColumn && columns != null && !columns.Contains(pocoColumn.ColumnName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    object value = pocoColumn.GetColumnValue(pd, poco, database.ProcessMapper);

                    if (pocoColumn.VersionColumn)
                    {
                        versionName = pocoColumn.ColumnName;
                        versionValue = value;
                        if (pocoColumn.VersionColumnType == VersionColumnType.Number)
                        {
                            versionColumnType = VersionColumnType.Number;
                            value = Convert.ToInt64(value) + 1;
                        }
                        else if (pocoColumn.VersionColumnType == VersionColumnType.RowVersion)
                        {
                            versionColumnType = VersionColumnType.RowVersion;
                            continue;
                        }
                    }

                    // Build the sql
                    if (index > 0)
                        sb.Append(", ");
                    sb.AppendFormat("{0} = @{1}", database.DatabaseType.EscapeSqlIdentifier(pocoColumn.ColumnName), index++);

                    rawvalues.Add(value);
                }

                if (sb.Length == 0)
                {
                    return new PreparedUpdateStatement();
                }

                var sql = $"UPDATE {database.DatabaseType.EscapeTableName(tableName)} SET {sb} WHERE {BuildPrimaryKeySql(database, primaryKeyValuePairs, ref index)}";

                rawvalues.AddRange(primaryKeyValuePairs.Select(keyValue => keyValue.Value));

                if (!string.IsNullOrEmpty(versionName))
                {
                    sql += $" AND {database.DatabaseType.EscapeSqlIdentifier(versionName)} = @{index++}";
                    rawvalues.Add(versionValue);
                }

                return new PreparedUpdateStatement
                {
                    PocoData = pd,
                    Rawvalues = rawvalues,
                    Sql = sql,
                    VersionName = versionName,
                    VersionValue = versionValue,
                    VersionColumnType = versionColumnType,
                    PrimaryKeyValuePairs = primaryKeyValuePairs
                };
            }

            internal class PreparedUpdateStatement
            {
                public PocoData PocoData { get; set; }
                public string VersionName { get; set; }
                public object VersionValue { get; set; }
                public VersionColumnType VersionColumnType { get; set; }
                public string Sql { get; set; }
                public List<object> Rawvalues { get; set; }
                public Dictionary<string, object> PrimaryKeyValuePairs { get; set; }
            }
        }
    }
}
