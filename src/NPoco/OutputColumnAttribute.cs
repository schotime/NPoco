using System;

namespace NPoco
{
    /// <summary>
    /// Marks a property as a column that should be in the OUTPUT clause of an Insert / Update. This means
    /// the property will be automatically updated after the Insert / Update operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OutputColumnAttribute : ColumnAttribute
    {
        public OutputColumnAttribute()
            : this("")
        {
        }


        public OutputColumnAttribute(OutputColumnMode modes)
            : this("", modes)
        {

            // IsResultColumn = isResultColumn;
        }

        public OutputColumnAttribute(string name)
            : this(name, OutputColumnMode.Insert | OutputColumnMode.Update)
        {

        }

        public OutputColumnAttribute(string name, OutputColumnMode modes)
            : base(name)
        {
            OutputColumnMode = modes;
            // IsResultColumn = isResultColumn;
        }

        public OutputColumnMode OutputColumnMode { get; set; }

        ///// <summary>
        ///// Specifies whether the column is a result only column, which means it won't be included in 
        ///// Select / Insert / Update statements.
        ///// </summary>
        //public bool IsResultColumn { get; set; }

    }

    [Flags]
    public enum OutputColumnMode
    {
        None = 0,
        Insert = 1,
        Update = 1 << 1,
    }
}