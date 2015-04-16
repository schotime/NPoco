using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPoco
{
    public class InsertStatements
    {
        public class PreparedInsertSql
        {
            public PocoData pocoData { get; set; }
            public string versionName { get; set; }
            public string sql { get; set; }
            public List<object> rawvalues { get; set; }
        }

        public static PreparedInsertSql PrepareInsertSql<T>(Database database, string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            database.OpenSharedConnectionInternal();

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
                    string autoIncExpression = database._dbType.GetAutoIncrementExpression(pd.TableInfo);
                    if (autoIncExpression != null)
                    {
                        names.Add(i.Key);
                        values.Add(autoIncExpression);
                    }
                    continue;
                }

                names.Add(database._dbType.EscapeSqlIdentifier(i.Key));
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
                outputClause = database._dbType.GetInsertOutputClause(primaryKeyName);
            }

            if (names.Count != 0)
            {
                sql = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3})",
                    database._dbType.EscapeTableName(tableName),
                    string.Join(",", names.ToArray()),
                    outputClause,
                    string.Join(",", values.ToArray()));
            }
            else
            {
                sql = database._dbType.GetDefaultInsertSql(tableName, names.ToArray(), values.ToArray());
            }
            return new PreparedInsertSql()
            {
                pocoData = pd,
                sql = sql,
                rawvalues = rawvalues,
                versionName = versionName
            };
        }

        public static object AssignNonIncrementPrimaryKey<T>(string primaryKeyName, T poco, PreparedInsertSql preparedSql)
        {
            PocoColumn pkColumn;
            if (primaryKeyName != null && preparedSql.pocoData.Columns.TryGetValue(primaryKeyName, out pkColumn))
                return pkColumn.GetValue(poco);
            return null;
        }

        public static void AssignVersion<T>(T poco, PreparedInsertSql preparedSql)
        {
            if (!string.IsNullOrEmpty(preparedSql.versionName))
            {
                PocoColumn pc;
                if (preparedSql.pocoData.Columns.TryGetValue(preparedSql.versionName, out pc))
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
                if (preparedSql.pocoData.Columns.TryGetValue(primaryKeyName, out pc))
                {
                    pc.SetValue(poco, pc.ChangeType(id));
                }
            }
        }
    }
}
