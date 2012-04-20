using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute(string primaryKey)
        {
            Value = primaryKey;
            autoIncrement = true;
        }

        public string Value { get; private set; }
        public string sequenceName { get; set; }
        public bool autoIncrement { get; set; }
    }
}