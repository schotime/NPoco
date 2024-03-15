namespace NPoco.DbSpecific.Postgresql
{
    public class OnConflictDoNothingStatementHook : AlterStatementHook
    {
        public override PreparedInsertStatement AlterInsert(PreparedInsertStatement preparedInsertStatement)
        {
            preparedInsertStatement.Sql += " ON CONFLICT DO NOTHING";
            return preparedInsertStatement;
        }
    }
}