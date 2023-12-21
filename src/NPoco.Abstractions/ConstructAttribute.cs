using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ConstructAttribute : Attribute
    {
        public ConstructAttribute() { }
    }
}