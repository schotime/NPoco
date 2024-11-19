namespace NPoco
{
    public interface IAlterStatementHook
    {
        PreparedInsertStatement AlterInsert(IDatabase database, PreparedInsertStatement preparedInsertStatement);
        PreparedUpdateStatement AlterUpdate(IDatabase database, PreparedUpdateStatement preparedUpdateStatement);
    }

    public abstract class AlterStatementHook : IAlterStatementHook
    {
        public virtual PreparedInsertStatement AlterInsert(IDatabase database, PreparedInsertStatement preparedInsertStatement) => preparedInsertStatement;
        public virtual PreparedUpdateStatement AlterUpdate(IDatabase database, PreparedUpdateStatement preparedUpdateStatement) => preparedUpdateStatement;
    }
}