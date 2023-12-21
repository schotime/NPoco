using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute(string primaryKey)
        {
            Value = primaryKey;
            _autoIncrement = true;
        }

        public PrimaryKeyAttribute(string[] primaryKey) : this(string.Join(",", primaryKey))
        {            
        }

        public string Value { get; private set; }
        public string SequenceName { get; set; }
        private bool _autoIncrement;
        public bool AutoIncrement
        {
            get { return _autoIncrement; }
            set
            {
                _autoIncrement = value;
                if (value && Value.Contains(","))
                {
                    throw new InvalidOperationException("Cannot set AutoIncrement to true when the primary key is a Composite Key");
                }
            }
        }
        public bool UseOutputClause { get; set; }
    }
}