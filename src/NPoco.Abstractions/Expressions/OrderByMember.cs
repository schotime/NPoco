using System;

namespace NPoco.Expressions
{
    public class OrderByMember
    {
        public Type EntityType { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }
        public string AscDesc { get; set; }
    }
}
