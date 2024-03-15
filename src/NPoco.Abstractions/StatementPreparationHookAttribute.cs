using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class StatementPreparationHookAttribute : Attribute
    {
        public abstract IAlterStatementHook AlterStatementHook { get; }
    }
}