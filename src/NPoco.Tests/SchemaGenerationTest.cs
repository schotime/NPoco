using System;
using System.Linq;
using System.Collections.Generic;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests
{
    [TestFixture]
    //[NUnit.Framework.Ignore("Appearently the decorated syntax and fluent syntax are some how conflicting.")]
    public class SchemaGenerationTest : BaseDBDecoratedTest
    {
        [Test]
        public void SimpleSchemaTest()
        {

            var tableInfo = new TableInfo();

            tableInfo.TableName = "simple";

            var columns = new Dictionary<String, PocoColumn>();

            var col = new PocoColumn();

            col.ColumnName = "string1";

            AddColumn(columns, "string", typeof(string));
            AddColumn(columns, "byte", typeof(byte[]));
            AddColumn(columns, "bool", typeof(bool));
            AddColumn(columns, "decimal", typeof(decimal));
            AddColumn(columns, "datetime", typeof(DateTime));
            AddColumn(columns, "double", typeof(double));
            AddColumn(columns, "guid", typeof(Guid));
            AddColumn(columns, "int", typeof(int));
            AddColumn(columns, "short", typeof(short));
            AddColumn(columns, "long", typeof(long));
            AddColumn(columns, "single", typeof(Single));

            var pocoData = new MockPocoData(tableInfo, columns);

            Database.CreateSchema(pocoData);

            EnsureSchemaMatch(Database, pocoData);

        }

        void AddColumn(IDictionary<string, PocoColumn> columns, string columnName, Type columnType)
        {
            AddColumn(columns, columnName, columnType, false, 1, 1);
        }

        void AddColumn(IDictionary<string, PocoColumn> columns, string columnName, Type columnType, bool identityColumn, int identitySeed, int identityIncrement)
        {
            var col = new PocoColumn();
            col.ColumnName = columnName;
            col.ColumnType = columnType;
            col.IdentityColumn = identityColumn;
            col.IdentitySeed = identitySeed;
            col.IdentityIncrement = identityIncrement;

            columns.Add(columnName, col);
        }

        void EnsureSchemaMatch(IDatabase db, IPocoData pocoData)
        {
            var dbType = db.DatabaseType;

            if (dbType is NPoco.DatabaseTypes.SqlServerDatabaseType)
            {
                //var columns = Database.FetchBy<Common.InformationSchema.Column>(y => y.Where(y => (y.TABLE_NAME == "test")));
                var columns = Database.FetchBy<Common.InformationSchema.Column>(y => y.Where(x => x.TABLE_NAME == pocoData.TableInfo.TableName));

                Assert.AreEqual(columns.Count, pocoData.Columns.Count);
                foreach (var pocoColumn in pocoData.Columns)
                {

                    var realColumn = (
                        from col in columns
                        where col.COLUMN_NAME == pocoColumn.Value.ColumnName
                        select col).First();

                    string dataType = realColumn.DATA_TYPE.ToLower();

                    if (pocoColumn.Value.ColumnType == typeof (string))
                    {
                        Assert.AreEqual("varchar", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof (byte[]))
                    {
                        Assert.AreEqual("image", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(bool))
                    {
                        Assert.AreEqual("bit", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(decimal))
                    {
                        Assert.AreEqual("decimal", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(DateTime))
                    {
                        Assert.AreEqual("datetime", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(double))
                    {
                        Assert.AreEqual("float", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(Guid))
                    {
                        Assert.AreEqual("uniqueidentifier", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(short))
                    {
                        Assert.AreEqual("smallint", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(int))
                    {
                        Assert.AreEqual("int", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(long))
                    {
                        Assert.AreEqual("bigint", dataType);
                    }
                    else if (pocoColumn.Value.ColumnType == typeof(Single))
                    {
                        Assert.AreEqual("real", dataType);
                    }
                    else
                    {
                        throw new NotSupportedException(pocoColumn.Value.ColumnType.Name);
                    } 
                }
            }
            else
            {
                throw new NotImplementedException(dbType.GetType().Name);
            }

        }
    }
}
