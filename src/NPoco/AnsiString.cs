namespace NPoco
{
    public class AnsiString
    {
        public AnsiString(string str)
        {
            Value = str;
        }
        public string Value { get; private set; }
    }
}