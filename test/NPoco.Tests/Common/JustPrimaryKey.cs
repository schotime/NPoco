using NPoco;

namespace NPoco.Tests.Common
{
    [TableName("JustPrimaryKey"), PrimaryKey("Id", AutoIncrement = true)]
    public class JustPrimaryKey
    {
        public int Id { get; set; }
    }
}