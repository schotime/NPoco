using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReferenceColumnAttribute : ColumnAttribute
    {
        public ReferenceColumnAttribute() { }
        public ReferenceColumnAttribute(string name) : base(name) { }
    }
}