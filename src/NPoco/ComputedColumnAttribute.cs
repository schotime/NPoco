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

    public enum ComputedColumnType
    {
        /// <summary>
        /// Always considered as a computed column
        /// </summary>
        Always,
        /// <summary>
        /// Only considered a Computed column for inserts, Updates will not consider this column to be computed
        /// </summary>
        ComputedOnInsert,
        /// <summary>
        /// Only considered a Computed column for updates, Inserts will not consider this column to be computed
        /// </summary>
        ComputedOnUpdate
    }
}