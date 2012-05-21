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
        public bool ForceToUtc { get; set; }
        public Type ColumnType { get; set; }

        public static ColumnInfo FromProperty(PropertyInfo pi)
        {
            // Check if declaring poco has [Explicit] attribute
            bool ExplicitColumns = pi.DeclaringType.GetCustomAttributes(typeof(ExplicitColumnsAttribute), true).Length > 0;

            // Check for [Column]/[Ignore] Attributes
            var ColAttrs = pi.GetCustomAttributes(typeof(ColumnAttribute), true);
            if (ExplicitColumns)
            {
                if (ColAttrs.Length == 0)
                    return null;
            }
            else
            {
                if (pi.GetCustomAttributes(typeof(IgnoreAttribute), true).Length != 0)
                    return null;
            }

            ColumnInfo ci = new ColumnInfo();

            // Read attribute
            if (ColAttrs.Length > 0)
            {
                var colattr = (ColumnAttribute)ColAttrs[0];

                ci.ColumnName = colattr.Name ?? pi.Name;
                ci.ForceToUtc = colattr.ForceToUtc;
                if ((colattr as ResultColumnAttribute) != null)
                    ci.ResultColumn = true;
            }
            else
            {
                ci.ColumnName = pi.Name;
                ci.ForceToUtc = false;
                ci.ResultColumn = false;
            }

            var columnTypeAttr = pi.GetCustomAttributes(typeof(ColumnTypeAttribute), true);
            if (columnTypeAttr.Any())
            {
                ci.ColumnType = ((ColumnTypeAttribute)columnTypeAttr[0]).Type;
            }
            
            return ci;

        }

    }
}
