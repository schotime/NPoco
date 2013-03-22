namespace NPoco
{
    public class DeleteContext
    {
        public DeleteContext(object poco, string tableName, string primaryKeyName, object primaryKeyValue)
        {
            Poco = poco;
            TableName = tableName;
            PrimaryKeyName = primaryKeyName;
            PrimaryKeyValue = primaryKeyValue;
        }

        public object Poco { get; private set; }
        public string TableName { get; private set; }
        public string PrimaryKeyName { get; private set; }
        public object PrimaryKeyValue { get; private set; }
    }
}