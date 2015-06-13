using System.Collections.Generic;

namespace NPoco.Tests.NewMapper.Models
{
    [TableName("Ones"), PrimaryKey("OneId")]
    public class One
    {
        public int OneId { get; set; }
        public string Name { get; set; }

        [Reference(ReferenceMappingType.Many, Name = "OneId", ReferenceName = "OneId")]
        public List<Many> Items { get; set; }
    }
}