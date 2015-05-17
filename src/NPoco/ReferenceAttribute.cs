using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReferenceAttribute : ColumnAttribute
    {
    }
}
