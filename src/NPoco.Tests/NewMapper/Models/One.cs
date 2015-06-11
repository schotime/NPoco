using System.Collections.Generic;

namespace NPoco.Tests.NewMapper.Models
{
    [TableName("Ones"), PrimaryKey("Id")]
    public class One
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference(ReferenceMappingType.Many, Name =  "Id", ReferenceName = "One")]
        public List<Many> Items { get; set; }
    }
}