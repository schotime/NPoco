using System;

namespace NPoco.Linq
{
    public class JoinData
    {
        public string OnSql { get; set; }
        public Type Type { get; set; }
        public string BaseName { get; set; }
    }
}