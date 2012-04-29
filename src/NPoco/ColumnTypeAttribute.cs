using System;

namespace NPoco
{
    public class ColumnTypeAttribute : Attribute
    {
        public Type Type { get; set; }
        public ColumnTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
