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
    }
}