using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute(string primaryKey)
        {
            Value = primaryKey;
            AutoIncrement = true;
        }

        public string Value { get; private set; }
        public string SequenceName { get; set; }
        public bool AutoIncrement { get; set; }
    }
}