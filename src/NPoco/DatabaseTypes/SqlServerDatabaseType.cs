using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace NPoco.DatabaseTypes
{
    public class SqlServerDatabaseType : DatabaseType
    {
        private static readonly Regex OrderByAlias = new Regex(@"(^.* )([\w\""\[\]]*\.)(.*$)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public override bool UseColumnAliases()
        {
            return true;
        }

        public override string BuildPageQuery(long skip, long take, PagingHelper.SQLParts parts, ref object[] args)
        {
            parts.sqlOrderBy = string.IsNullOrEmpty(parts.sqlOrderBy) ? null : OrderByAlias.Replace(parts.sqlOrderBy, "$1$3");
            var sqlPage = string.Format("SELECT {4} FROM (SELECT ROW_NUMBER() OVER ({0}) poco_rn, poco_base.* \nFROM ( \n{1}) poco_base ) poco_paged \nWHERE poco_rn > {2} AND poco_rn <= {3} \nORDER BY poco_rn",
                                                                    parts.sqlOrderBy ?? "ORDER BY (SELECT NULL /*poco_dual*/)", parts.sqlUnordered, skip, skip + take, parts.sqlColumns);
            args = args.Concat(new object[] { skip, skip + take }).ToArray();

            return sqlPage;
        }

        public override object ExecuteInsert<T>(Database db, IDbCommand cmd, string primaryKeyName, T poco, object[] args)
        {
            //var pocodata = PocoData.ForType(typeof(T), db.PocoDataFactory);
            //var sql = string.Format("SELECT * FROM {0} WHERE {1} = SCOPE_IDENTITY()", EscapeTableName(pocodata.TableInfo.TableName), EscapeSqlIdentifier(primaryKeyName));
            //return db.SingleInto(poco, ";" + cmd.CommandText + ";" + sql, args);
            cmd.CommandText += ";SELECT SCOPE_IDENTITY();";
            return db.ExecuteScalarHelper(cmd);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

        public override void InsertBulk<T>(IDatabase db, IEnumerable<T> pocos)
        {
            SqlBulkCopyHelper.BulkInsert(db, pocos);
        }

        public override IsolationLevel GetDefaultTransactionIsolationLevel()
        {
            return IsolationLevel.ReadCommitted;
        }

        public override DbType? LookupDbType(Type type, string name)
        {
            if (type == typeof (TimeSpan) || type == typeof(TimeSpan?))
                return null;
            
            return base.LookupDbType(type, name);
        }

        public override string GetProviderName()
        {
            return "System.Data.SqlClient";
        }

        // this should be overriden in the other SQL server classes to return the new column types 
        public virtual SqlDbType GetSQLColumnType(PocoColumn pocoColumn)
        {
            var dbType = LookupDbType(pocoColumn.ColumnType, pocoColumn.ColumnName);
            switch (dbType.Value)
            {
                case DbType.StringFixedLength:
                case DbType.String:
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                    return SqlDbType.VarChar;
                case DbType.Binary:
                case DbType.SByte:
                case DbType.Byte:
                    return SqlDbType.Image;
                case DbType.Boolean:
                    return SqlDbType.Bit;
                case DbType.Currency:
                case DbType.Decimal:
                    return SqlDbType.Decimal;
                case DbType.Time:
                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                    return SqlDbType.DateTime;
                //case DbType.DateTimeOffset: not supported
                case DbType.Double:
                    return SqlDbType.Float;
                case DbType.Guid:
                    return SqlDbType.UniqueIdentifier;
                case DbType.Int16:
                    return SqlDbType.SmallInt;
                case DbType.Int32:
                    return SqlDbType.Int;
                case DbType.Int64:
                    return SqlDbType.BigInt;
                case DbType.Single:
                    return SqlDbType.Real;
                default:
                    throw new NotImplementedException(dbType.ToString());
            }
        }

        private string GetColumnSchema(PocoColumn pocoColumn, bool newColumn)
        {

            var builder = new System.Text.StringBuilder();

            var sqlDbType = GetSQLColumnType(pocoColumn);

            builder.AppendFormat("[{0}] {1}", pocoColumn.ColumnName, sqlDbType);

            switch (sqlDbType)
            {
                case SqlDbType.VarChar:
                    builder.Append("(8000)");
                    break;
                case SqlDbType.Decimal:
                    builder.Append("(19, 4)"); //this should probably be end user customizable.
                    break;
            }

            if (newColumn && pocoColumn.IdentityColumn)
            {
                builder.AppendFormat(" IDENTITY({0},{1})", pocoColumn.IdentitySeed, pocoColumn.IdentityIncrement);
            }

            return builder.ToString();
        }

        public override void CreateSchema(Database db, IPocoData pocoData)
        {

            if (pocoData.Columns.Count == 0)
            {
                throw new Exception("No columns on pocoData, can not create table.");
            }

            IDbCommand cmd = db.Connection.CreateCommand();

            var commandBuilder = new System.Text.StringBuilder();

            //doesnt look like NPoco supports the table container not being dbo, defaulting for now.
            string tableContainer = "dbo";
            string tableName = pocoData.TableInfo.TableName;

            string fullTableName = tableName;

            //TODO add support for foreign keyed columns
            //List<IColumnSchema> colForeignKeyColumns = (from col in Schema.Columnswhere !string.IsNullOrEmpty(col.ForeignKeyTable)col).ToList;

            //TODO add support for indexing columns
            //dynamic colIndexes = Schema.IndexesCalculated;


            commandBuilder.AppendFormat(" IF NOT (EXISTS (SELECT * ");
            commandBuilder.AppendFormat(" FROM INFORMATION_SCHEMA.TABLES ");
            commandBuilder.AppendFormat(" WHERE TABLE_SCHEMA = '{0}' ", tableContainer);
            commandBuilder.AppendFormat(" AND  TABLE_NAME = '{0}')) ", tableName);
            commandBuilder.AppendFormat(" BEGIN ");
            commandBuilder.AppendFormat("     CREATE TABLE {0}( ", fullTableName);
            commandBuilder.AppendFormat(string.Join(" , ", from PocoColumn col in pocoData.Columns.Values select GetColumnSchema(col, true)));
            //TODO add support for foreign keyed columns
            //if (colForeignKeyColumns.Count > 0) {
            //    commandBuilder.AppendFormat(" , {0}", string.Join(" , ", from col in colForeignKeyColumnscol.MSSqlForeignKeySchema(Schema)));
            //}

            if ((pocoData.TableInfo.PrimaryKey ?? "") != "")
            {
                commandBuilder.AppendFormat(" , PRIMARY KEY ({0}) ", pocoData.TableInfo.PrimaryKey);
            }
            commandBuilder.AppendFormat("     ) ");
            commandBuilder.AppendFormat(" END ");
            commandBuilder.AppendFormat(" ELSE ");
            commandBuilder.AppendFormat(" BEGIN ");


            foreach (PocoColumn pocoColumn in pocoData.Columns.Values)
            {

                ////TODO Implement maxLength
                //dynamic oMaxLength = cmd.CreateParameter();

                //oMaxLength.ParameterName = string.Format("Column{0}MaxLength", pocoColumn.ColumnName);
                //if (pocoColumn.MaxLengthCalculated == 0) {
                //    oMaxLength.Value = DBNull.Value;
                //} else {
                //    oMaxLength.Value = pocoColumn.MaxLengthCalculated;
                //}
                //oCommand.Parameters.Add(oMaxLength);
                string columnSchema = GetColumnSchema(pocoColumn, false);

                commandBuilder.AppendFormat(" IF NOT (EXISTS (SELECT * ");
                commandBuilder.AppendFormat(" FROM INFORMATION_SCHEMA.COLUMNS ");
                commandBuilder.AppendFormat(" WHERE TABLE_SCHEMA = '{0}' ", tableContainer);
                commandBuilder.AppendFormat(" AND  TABLE_NAME = '{0}' ", tableName);
                commandBuilder.AppendFormat(" AND  COLUMN_NAME = '{0}')) ", pocoColumn.ColumnName);
                commandBuilder.AppendFormat(" BEGIN ");
                commandBuilder.AppendFormat("     ALTER TABLE {0} ", fullTableName);
                commandBuilder.AppendFormat("     ADD {0} ", columnSchema);
                commandBuilder.AppendFormat(" END ");
                commandBuilder.AppendFormat(" ELSE ");
                commandBuilder.AppendFormat(" BEGIN ");

                commandBuilder.AppendFormat("     IF NOT (EXISTS (SELECT * ");
                commandBuilder.AppendFormat("     FROM INFORMATION_SCHEMA.COLUMNS ");
                commandBuilder.AppendFormat("     WHERE TABLE_SCHEMA = '{0}' ", tableContainer);
                commandBuilder.AppendFormat("     AND  TABLE_NAME = '{0}' ", tableName);
                commandBuilder.AppendFormat("     AND  DATA_TYPE = '{0}' ", GetSQLColumnType(pocoColumn));
                //TODO implement maxLength
                //commandBuilder.AppendFormat("     AND  Coalesce(CHARACTER_MAXIMUM_LENGTH, '') = Coalesce(@{0}, '') ", oMaxLength.ParameterName);

                commandBuilder.AppendFormat("     )) ");
                commandBuilder.AppendFormat("     BEGIN ");
                commandBuilder.AppendFormat("         ALTER TABLE {0} ", fullTableName);
                commandBuilder.AppendFormat("         ALTER COLUMN {0} ", columnSchema);
                commandBuilder.AppendFormat("     END ");

                commandBuilder.AppendFormat(" END ");


            }
            //TODO implement foreign keys
            //foreach (void oForeignKeyedColumn_loopVariable in colForeignKeyColumns) {
            //    oForeignKeyedColumn = oForeignKeyedColumn_loopVariable;
            //    commandBuilder.AppendFormat("    IF NOT (EXISTS ( ");
            //    commandBuilder.AppendFormat("    SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS ");
            //    commandBuilder.AppendFormat("    WHERE CONSTRAINT_NAME = '{0}' ", oForeignKeyedColumn.ForeignKeyName(Schema));
            //    commandBuilder.AppendFormat("    )) ");
            //    commandBuilder.AppendFormat("    BEGIN ");
            //    commandBuilder.AppendFormat("    ALTER TABLE [{0}] ADD ", Schema.TableName);
            //    commandBuilder.AppendFormat("    {0} ", oForeignKeyedColumn.MSSqlForeignKeySchema(Schema));
            //    commandBuilder.AppendFormat("    END ");
            //}

            commandBuilder.AppendFormat(" END ");

            //TODO implement indexes
            //foreach (  oIndex_loopVariable in colIndexes) {
            //    oIndex = oIndex_loopVariable;
            //    //SELECT * 
            //    //FROM sys.indexes 
            //    //WHERE name='{0}' AND
            //    //object_id = OBJECT_ID('{0}')

            //    commandBuilder.AppendFormat("    IF NOT (EXISTS ( ");
            //    commandBuilder.AppendFormat("    SELECT * FROM sys.indexes ");
            //    commandBuilder.AppendFormat("    WHERE name = '{0}' ", oIndex.IndexName(Schema));
            //    commandBuilder.AppendFormat("    AND object_id = OBJECT_ID('{0}') ", Schema.TableName);
            //    commandBuilder.AppendFormat("    )) ");
            //    commandBuilder.AppendFormat("    BEGIN ");
            //    commandBuilder.AppendFormat("    {0} ", oIndex.MSSqlSchema(Schema));
            //    commandBuilder.AppendFormat("    END ");
            //}

            cmd.CommandText = commandBuilder.ToString();

            db.ExecuteNonQueryHelper(cmd);

        }

    }
}