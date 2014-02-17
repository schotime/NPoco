using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AliasAttribute : Attribute
    {
        public AliasAttribute() { }
        public AliasAttribute(string name) { Name = name; }
        public string Name { get; set; }
    }
}
