using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReferenceAttribute : Attribute
    {
        public readonly ReferenceType ReferenceType;

        public ReferenceAttribute() : this(ReferenceType.Foreign)
        {
        }

        public ReferenceAttribute(ReferenceType referenceType)
        {
            ReferenceType = referenceType;
        }
        
        /// <summary>
        /// The property name (case sensitive) that links the relationship.
        /// </summary>
        public string ReferenceMemberName { get; set; }

        /// <summary>
        /// The database column name that maps to the property.
        /// </summary>
        public string ColumnName { get; set; }
    }

    public enum ReferenceType
    {
        None,
        OneToOne,
        Foreign,
        Many
    }
}
