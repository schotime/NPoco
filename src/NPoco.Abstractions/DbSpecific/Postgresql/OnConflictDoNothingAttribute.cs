namespace NPoco.DbSpecific.Postgresql
{
    public class OnConflictDoNothingAttribute : StatementPreparationHookAttribute
    {
        public override IAlterStatementHook AlterStatementHook => new OnConflictDoNothingStatementHook();
    }
}
