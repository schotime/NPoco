using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServerRawVersionAttribute : Attribute
    {
        public ServerRawVersionAttribute(string serverRawVersionColumn)
        {
            Value = serverRawVersionColumn;
        }

        public string Value { get; private set; }
    }
}
