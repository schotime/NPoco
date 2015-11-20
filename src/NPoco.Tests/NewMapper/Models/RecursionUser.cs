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
}