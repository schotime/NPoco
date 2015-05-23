using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NPoco
{
    public class ColumnInfo
    {
        public string ColumnName { get; set; }
        public string ColumnAlias { get; set; }
        public bool ResultColumn { get; set; }
        public bool ComputedColumn { get; set; }
        public bool IgnoreColumn { get; set; }
        public bool VersionColumn { get; set; }
        public VersionColumnType VersionColumnType { get; set; }
        public bool ForceToUtc { get; set; }
        public Type ColumnType { get; set; }
        public bool ComplexMapping { get; set; }
        public string ComplexPrefix { get; set; }
        public ReferenceMappingType ReferenceMappingType { get; set; }
        public string ReferenceMemberName { get; set; }

        public static ColumnInfo FromMemberInfo(MemberInfo mi)
        {
            var ci = new ColumnInfo();

            var attrs = Attribute.GetCustomAttributes(mi, true);
            var colAttrs = attrs.OfType<ColumnAttribute>();
            var columnTypeAttrs = attrs.OfType<ColumnTypeAttribute>();
            var ignoreAttrs = attrs.OfType<IgnoreAttribute>();
            var complex = attrs.OfType<ComplexMappingAttribute>();
            var reference = attrs.OfType<ReferenceAttribute>();

          
            // Check if declaring poco has [ExplicitColumns] attribute
            var explicitColumns = mi.DeclaringType.GetCustomAttributes(typeof(ExplicitColumnsAttribute), true).Any();

            var aliasColumn = (AliasAttribute) Attribute.GetCustomAttributes(mi, typeof(AliasAttribute), true).FirstOrDefault();
            // Ignore column if declarying poco has [ExplicitColumns] attribute
            // and property doesn't have an explicit [Column] attribute,
            // or property has an [Ignore] attribute
            if ((explicitColumns && !colAttrs.Any()) || ignoreAttrs.Any())
            {
                ci.IgnoreColumn = true;
            }

            if (reference.Any())
            {
                ci.ReferenceMappingType = reference.First().ReferenceMappingType;
                ci.ReferenceMemberName = reference.First().ReferenceName ?? "Id";
                ci.ColumnName = reference.First().Name ?? mi.Name + "Id";
                return ci;
            }

            if (complex.Any())
            {
                ci.ComplexMapping = true;
                ci.ComplexPrefix = complex.First().CustomPrefix;
                return ci;
            }

            if (mi.GetMemberInfoType().IsAClass())
            {
                ci.ComplexMapping = true;
                return ci;
            }

            // Read attribute
            if (colAttrs.Any())
            {
                var colattr = colAttrs.First();
                ci.ColumnName = colattr.Name ?? mi.Name;
                ci.ForceToUtc = colattr.ForceToUtc;
                ci.ResultColumn = colattr is ResultColumnAttribute;
                ci.VersionColumn = colattr is VersionColumnAttribute;
                if (ci.VersionColumn)
                {
                    ci.VersionColumnType = ((VersionColumnAttribute) colattr).VersionColumnType;
                }
                ci.ComputedColumn = colattr is ComputedColumnAttribute;
                ci.ColumnAlias = aliasColumn != null ? aliasColumn.Alias : null;
            }
            else
            {
                ci.ColumnName = mi.Name;
            }

            if (columnTypeAttrs.Any())
            {
                ci.ColumnType = columnTypeAttrs.First().Type;
            }

            return ci;
        }
    }

    public class ComplexMappingAttribute : Attribute
    {
        public string CustomPrefix { get; set; }

        public ComplexMappingAttribute(string customPrefix)
        {
            CustomPrefix = customPrefix;
        }
    }
}
