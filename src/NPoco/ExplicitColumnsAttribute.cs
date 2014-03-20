using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExplicitColumnsAttribute : Attribute
    {
        public bool ApplyToBase { get; set; }
    }
}
