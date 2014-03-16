using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ResultColumnAttribute : ColumnAttribute
    {
        public bool IncludeInAutoSelect { get; set; }
        public ResultColumnAttribute() { }
        public ResultColumnAttribute(string name) : base(name) { }
    }
}