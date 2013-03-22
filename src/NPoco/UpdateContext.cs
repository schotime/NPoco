using System.Collections.Generic;

namespace NPoco
{
    public class UpdateContext
    {
        public UpdateContext(object poco, string tableName, string primaryKeyName, object primaryKeyValue, IEnumerable<string> columnsToUpdate)
        {
            Poco = poco;
            TableName = tableName;
            PrimaryKeyName = primaryKeyName;
            PrimaryKeyValue = primaryKeyValue;
            ColumnsToUpdate = columnsToUpdate;
        }

        public object Poco { get; private set; }
        public string TableName { get; private set; }
        public string PrimaryKeyName { get; private set; }
        public object PrimaryKeyValue { get; private set; }
        public IEnumerable<string> ColumnsToUpdate { get; private set; }
    }
}