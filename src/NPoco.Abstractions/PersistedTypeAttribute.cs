using System;
using System.Collections.Generic;
using System.Linq;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PersistedTypeAttribute : Attribute
    {
        public Type PersistedType { get; set; }

        public PersistedTypeAttribute(Type persistedType)
        {
            PersistedType = persistedType;
        }
    }
}
