using NPoco;

namespace NPoco.Tests.NewMapper.Models
{
    [PrimaryKey("MoneyId")]
    public class Money
    {
        public int MoneyId { get; set; }

        [ComplexMapping]
        public Money2 Money2 { get; set; }

        public decimal Value { get; set; }
        public string Currency { get; set; }
    }
}