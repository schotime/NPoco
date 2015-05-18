using System;
using System.Reflection;

namespace NPoco.Linq
{
    public class JoinData
    {
        public string OnSql { get; set; }
        public MemberInfo MemberInfo { get; set; }
    }
}