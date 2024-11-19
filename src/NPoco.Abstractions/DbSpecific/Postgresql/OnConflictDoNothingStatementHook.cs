namespace NPoco.DbSpecific.Postgresql
{
    public class OnConflictDoNothingStatementHook : AlterStatementHook
    {
        public override PreparedInsertStatement AlterInsert(IDatabase database, PreparedInsertStatement preparedInsertStatement)
        {
            preparedInsertStatement.Sql += " ON CONFLICT DO NOTHING";
            return preparedInsertStatement;
        }
    }
}