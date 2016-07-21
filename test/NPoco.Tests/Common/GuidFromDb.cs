using System;
using NPoco;

namespace NPoco.Tests.Common
{
    [TableName("GuidFromDb"), PrimaryKey("Id", AutoIncrement = true, UseOutputClause = true)]
    public class GuidFromDb
    {
        public Guid Id { get; private set; }
        public string Name { get; set; }
    }

    [TableName("IdGenerated"), PrimaryKey("Id", UseIdGenerator = true)]
    public class InsertFromIdGenerator
    {
        public long Id { get; private set; }
        public string Data { get; set; }
    }
}
