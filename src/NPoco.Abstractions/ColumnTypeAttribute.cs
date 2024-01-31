using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ColumnTypeAttribute : Attribute
    {
        public Type Type { get; set; }
        public ColumnTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
