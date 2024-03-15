namespace NPoco
{
    public interface IAlterStatementHook
    {
        PreparedInsertStatement AlterInsert(PreparedInsertStatement preparedInsertStatement);
        PreparedUpdateStatement AlterUpdate(PreparedUpdateStatement preparedUpdateStatement);
    }

    public abstract class AlterStatementHook : IAlterStatementHook
    {
        public virtual PreparedInsertStatement AlterInsert(PreparedInsertStatement preparedInsertStatement) => preparedInsertStatement;
        public virtual PreparedUpdateStatement AlterUpdate(PreparedUpdateStatement preparedUpdateStatement) => preparedUpdateStatement;
    }
}