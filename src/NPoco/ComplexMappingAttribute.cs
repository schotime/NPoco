using System;

namespace NPoco
{
    public class ComplexMappingAttribute : Attribute
    {
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