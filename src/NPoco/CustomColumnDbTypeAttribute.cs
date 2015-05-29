using System;
using System.Data;

namespace NPoco
{
    /// <summary>
    /// Column Type Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CustomColumnDbTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public DbType Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnTypeAttribute"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public CustomColumnDbTypeAttribute(DbType type)
        {
            Type = type;
        }
    }
}
