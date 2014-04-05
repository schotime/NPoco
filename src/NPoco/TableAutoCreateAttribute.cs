using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAutoCreateAttribute : Attribute
    {
        public TableAutoCreateAttribute(bool autoCreate)
        {
            Value = autoCreate;
        }
        public bool Value { get; private set; }
    }
}