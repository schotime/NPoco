using NPoco;

namespace NPoco.Tests.NewMapper.Models
{
    [PrimaryKey("")]
    public class NestedConvention
    {
        public string Name { get; set; }
        
        [ResultColumn, ComplexMapping]
        public Money Money { get; set; }

        public int MoneyId { get; set; }

        public Extra Extra { get; set; }
    }
}