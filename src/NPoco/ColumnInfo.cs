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
        public bool ResultColumn { get; set; }
        public bool IgnoreColumn { get; set; }
        public bool ForceToUtc { get; set; }
        public Type ColumnType { get; set; }
        public bool IncludeColumnInAutoQuery { get; set; }

        public ColumnInfo()
        {
            IncludeColumnInAutoQuery = true;
        }

        public static ColumnInfo FromMemberInfo(MemberInfo mi)
        {
            var ci = new ColumnInfo();

            var attrs = mi.GetCustomAttributes(true);
            var colAttrs = attrs.OfType<ColumnAttribute>();
            var columnTypeAttrs = attrs.OfType<ColumnTypeAttribute>();
            var ignoreAttrs = attrs.OfType<IgnoreAttribute>();

            // Check if declaring poco has [ExplicitColumns] attribute
            var explicitColumns = mi.DeclaringType.GetCustomAttributes(typeof(ExplicitColumnsAttribute), true).Any();

            // Ignore column if declarying poco has [ExplicitColumns] attribute
            // and property doesn't have an explicit [Column] attribute,
            // or property has an [Ignore] attribute
            if ((explicitColumns && !colAttrs.Any()) || ignoreAttrs.Any())
            {
                ci.IgnoreColumn = true;
            }
            ci.IncludeColumnInAutoQuery = true;

            // Read attribute
            if (colAttrs.Any())
            {
                var colattr = colAttrs.First();
                ci.ColumnName = colattr.Name ?? mi.Name;
                ci.ForceToUtc = colattr.ForceToUtc;

                var resultColumnAttr = colattr as ResultColumnAttribute;
                if (resultColumnAttr != null)
                {
                    ci.ResultColumn = true;
                    ci.IncludeColumnInAutoQuery = resultColumnAttr.IncludeInAutoSelect;
                }
            }
            else
            {
                ci.ColumnName = mi.Name;
                ci.ForceToUtc = false;
                ci.ResultColumn = false;
            }

            if (columnTypeAttrs.Any())
            {
                ci.ColumnType = columnTypeAttrs.First().Type;
            }

            return ci;
        }
    }
}
