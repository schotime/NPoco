using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    public partial class Database
    {
        private class InsertStatements
        {
            public class PreparedInsertSql
            {
                public PocoData PocoData { get; set; }
                public string VersionName { get; set; }
                public string Sql { get; set; }
                public List<object> Rawvalues { get; set; }
            }

            public static PreparedInsertSql PrepareInsertSql<T>(Database database, string tableName, string primaryKeyName, bool autoIncrement, T poco)
            {
                var pd = database.PocoDataFactory.ForObject(poco, primaryKeyName);
                var names = new List<string>();
                var values = new List<string>();
                var rawvalues = new List<object>();
                var index = 0;
                var versionName = "";

                foreach (var i in pd.Columns)
                {
                    // Don't insert result columns
                    if (i.Value.ResultColumn
                        || i.Value.ComputedColumn
                        || (i.Value.VersionColumn && i.Value.VersionColumnType == VersionColumnType.RowVersion))
                    {
                        continue;
                    }

                    // Don't insert the primary key (except under oracle where we need bring in the next sequence value)
                    if (autoIncrement && primaryKeyName != null && string.Compare(i.Key, primaryKeyName, true) == 0)
                    {
                        // Setup auto increment expression
                        string autoIncExpression = database.DatabaseType.GetAutoIncrementExpression(pd.TableInfo);
                        if (autoIncExpression != null)
                        {
                            names.Add(i.Key);
                            values.Add(autoIncExpression);
                        }
                        continue;
                    }

                    names.Add(database.DatabaseType.EscapeSqlIdentifier(i.Key));
                    values.Add(string.Format("{0}{1}", database._paramPrefix, index++));

                    object val = database.ProcessMapper(i.Value, i.Value.GetValue(poco));

                    if (i.Value.VersionColumn && i.Value.VersionColumnType == VersionColumnType.Number)
                    {
                        val = Convert.ToInt64(val) > 0 ? val : 1;
                        versionName = i.Key;
                    }

                    rawvalues.Add(val);
                }

                var sql = string.Empty;
                var outputClause = String.Empty;
                if (autoIncrement)
                {
                    outputClause = database.DatabaseType.GetInsertOutputClause(primaryKeyName);
                }

                if (names.Count != 0)
                {
                    sql = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3})",
                        database.DatabaseType.EscapeTableName(tableName),
                        string.Join(",", names.ToArray()),
                        outputClause,
                        string.Join(",", values.ToArray()));
                }
                else
                {
                    sql = database.DatabaseType.GetDefaultInsertSql(tableName, names.ToArray(), values.ToArray());
                }
                return new PreparedInsertSql()
                {
                    PocoData = pd,
                    Sql = sql,
                    Rawvalues = rawvalues,
                    VersionName = versionName
                };
            }

            public static object AssignNonIncrementPrimaryKey<T>(string primaryKeyName, T poco, PreparedInsertSql preparedSql)
            {
                PocoColumn pkColumn;
                if (primaryKeyName != null && preparedSql.PocoData.Columns.TryGetValue(primaryKeyName, out pkColumn))
                    return pkColumn.GetValue(poco);
                return null;
            }

            public static void AssignVersion<T>(T poco, PreparedInsertSql preparedSql)
            {
                if (!string.IsNullOrEmpty(preparedSql.VersionName))
                {
                    PocoColumn pc;
                    if (preparedSql.PocoData.Columns.TryGetValue(preparedSql.VersionName, out pc))
                    {
                        pc.SetValue(poco, pc.ChangeType(1));
                    }
                }
            }

            public static void AssignPrimaryKey<T>(string primaryKeyName, T poco, object id, PreparedInsertSql preparedSql)
            {
                if (primaryKeyName != null && id != null && id.GetType().IsValueType)
                {
                    PocoColumn pc;
                    if (preparedSql.PocoData.Columns.TryGetValue(primaryKeyName, out pc))
                    {
                        pc.SetValue(poco, pc.ChangeType(id));
                    }
                }
            }
        }
    }
}
