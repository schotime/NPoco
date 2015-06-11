namespace NPoco.Tests.NewMapper.Models
{
    [TableName("Manys"), PrimaryKey("Id")]
    public class Many
    {
        public int Id { get; set; }
        [Reference(ReferenceMappingType.Foreign, Name = "OneId", ReferenceName = "Id")]
        public One One { get; set; }
        public int Value { get; set; }
        public string Currency { get; set; }
    }
}