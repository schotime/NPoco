namespace NPoco
{
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