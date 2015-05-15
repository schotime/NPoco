namespace NPoco.Tests.NewMapper
{
    public class OldFakeReader : FakeReader
    {
        public override string GetName(int i)
        {
            switch (i)
            {
                case 0: return "Name";
                case 1: return "MoneyId";
                case 2: return "Value";
                case 3: return "Currency";
                case 4: return "Value";
                case 5: return "Currency";
            }
            return null;
        }
    }
}