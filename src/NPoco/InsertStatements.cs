using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NPoco
{
    public partial class Database
    {
        public class InsertStatements
        {
            public class PreparedInsertSql
            {
                public PocoData PocoData { get; set; }
                public string VersionName { get; set; }
                public string Sql { get; set; }
                public List<object> Rawvalues { get; set; }
            }

            public static PreparedInsertSql PrepareInsertSql<T>(Database database, PocoData pd, string tableName, string primaryKeyName, bool autoIncrement, T poco)
            {
                var names = new List<string>();
                var values = new List<string>();
                var rawvalues = new List<object>();
                var index = 0;
                var versionName = "";

                foreach (var pocoColumn in pd.Columns.Values)
                {
                    // Don't insert result columns
                    if (pocoColumn.ResultColumn
                        || (pocoColumn.ComputedColumn && (pocoColumn.ComputedColumnType == ComputedColumnType.Always || pocoColumn.ComputedColumnType == ComputedColumnType.ComputedOnInsert))
                        || (pocoColumn.VersionColumn && pocoColumn.VersionColumnType == VersionColumnType.RowVersion))
                    {
                        continue;
                    }

                    // Don't insert the primary key (except under oracle where we need bring in the next sequence value)
                    if (autoIncrement && primaryKeyName != null && string.Compare(pocoColumn.ColumnName, primaryKeyName, true) == 0)
                    {
                        // Setup auto increment expression
                        string autoIncExpression = database.DatabaseType.GetAutoIncrementExpression(pd.TableInfo);
                        if (autoIncExpression != null)
                        {
                            names.Add(pocoColumn.ColumnName);
                            values.Add(autoIncExpression);
                        }
                        continue;
                    }

                    names.Add(database.DatabaseType.EscapeSqlIdentifier(pocoColumn.ColumnName));
                    values.Add(string.Format("@{0}", index++));

                    object val;
                    if (pocoColumn.ReferenceType == ReferenceType.Foreign)
                    {
                        var member = pd.Members.Single(x => x.MemberInfoData == pocoColumn.MemberInfoData);
                        var column = member.PocoMemberChildren.Single(x => x.Name == member.ReferenceMemberName);
                        val = database.ProcessMapper(column.PocoColumn, column.PocoColumn.GetValue(poco));
                    }
                    else
                    {
                        val = database.ProcessMapper(pocoColumn, pocoColumn.GetValue(poco));
                    }

                    if (pocoColumn.VersionColumn && pocoColumn.VersionColumnType == VersionColumnType.Number)
                    {
                        val = Convert.ToInt64(val) > 0 ? val : 1;
                        versionName = pocoColumn.ColumnName;
                    }

                    rawvalues.Add(val);
                }

                var sql = string.Empty;
                var outputClause = String.Empty;
                if (autoIncrement || !string.IsNullOrEmpty(pd.TableInfo.SequenceName))
                {
                    outputClause = database.DatabaseType.GetInsertOutputClause(primaryKeyName, pd.TableInfo.UseOutputClause);
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
                    sql = database.DatabaseType.GetDefaultInsertSql(tableName, primaryKeyName, pd.TableInfo.UseOutputClause, names.ToArray(), values.ToArray());
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
                if (primaryKeyName != null && id != null && id.GetType().GetTypeInfo().IsValueType)
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
