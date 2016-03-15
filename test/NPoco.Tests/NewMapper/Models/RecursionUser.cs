using NPoco;

namespace NPoco.Tests.NewMapper.Models
{
    public class RecursionUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Reference(ReferenceMemberName = "Id")]
        public RecursionUser Supervisor { get; set; }
        [Reference(ReferenceMemberName = "Id")]
        public RecursionUser CreatedBy { get; set; }
    }

    public class RecursionUser2
    {
        [Alias("Id")]
        public int TheId { get; set; }
        [Column("Name")]
        public string TheName { get; set; }
        [Reference(ReferenceMemberName = "Id")]
        public RecursionUser2 Supervisor { get; set; }
        [Reference(ReferenceMemberName = "Id")]
        public RecursionUser2 CreatedBy { get; set; }
    }
}