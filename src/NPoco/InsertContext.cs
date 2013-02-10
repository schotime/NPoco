namespace NPoco
{
    public class InsertContext
    {
        public InsertContext(object poco, string tableName, bool autoIncrement, string primaryKeyName)
        {
            Poco = poco;
            TableName = tableName;
            AutoIncrement = autoIncrement;
            PrimaryKeyName = primaryKeyName;
        }

        public object Poco { get; private set; }
        public string TableName { get; private set; }
        public string PrimaryKeyName { get; private set; }
        public bool AutoIncrement { get; private set; }
    }
}