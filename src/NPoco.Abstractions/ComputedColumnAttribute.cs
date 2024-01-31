using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ComputedColumnAttribute : ColumnAttribute
    {
        public ComputedColumnType ComputedColumnType = ComputedColumnType.Always;

        public ComputedColumnAttribute() { }
        public ComputedColumnAttribute(string name) : base(name) { }
        public ComputedColumnAttribute(ComputedColumnType computedColumnType)
        {
            ComputedColumnType = computedColumnType;
        }
        public ComputedColumnAttribute(string name, ComputedColumnType computedColumnType) : base(name)
        {
            ComputedColumnType = computedColumnType;
        }
    }
}