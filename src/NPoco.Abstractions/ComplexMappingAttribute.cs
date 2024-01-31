using System;

namespace NPoco
{
    public class ComplexMappingAttribute : Attribute
    {
        public bool ComplexMapping { get; set; } = true;
        public string CustomPrefix { get; set; }

        public ComplexMappingAttribute()
        {
            
        }

        public ComplexMappingAttribute(string customPrefix)
        {
            CustomPrefix = customPrefix;
        }
    }
}