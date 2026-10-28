namespace NPoco
{
    public class BatchOptions
    {
        public BatchOptions()
        {
            BatchSize = 20;
            StatementSeperator = ";";
        }

        public int BatchSize { get; set; }
        public string StatementSeperator { get; set; }

        /// <summary>
        /// Combine each batch into a single INSERT INTO ... VALUES (...), (...) statement instead of
        /// one statement per row. Ignored for databases that don't support it (Oracle, Firebird, SQL CE).
        /// Subject to the database's parameter limit, so size BatchSize accordingly.
        /// </summary>
        public bool MultiRowValues { get; set; }
    }
}