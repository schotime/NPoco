using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReferenceAttribute : ColumnAttribute
    {
        public readonly ReferenceMappingType ReferenceMappingType;

        public ReferenceAttribute() : this(ReferenceMappingType.Foreign)
        {
        }

        public ReferenceAttribute(ReferenceMappingType referenceMappingType)
        {
            ReferenceMappingType = referenceMappingType;
        }

        public string ReferenceName { get; set; }
    }

    public enum ReferenceMappingType
    {
        None,
        OneToOne,
        Foreign
    }
}
