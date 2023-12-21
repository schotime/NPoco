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
        public ComputedColumnType ComputedColumnType { get; set; }
        public bool IgnoreColumn { get; set; }
        public bool VersionColumn { get; set; }
        public VersionColumnType VersionColumnType { get; set; }
        public bool ForceToUtc { get; set; } = true;
        public Type ColumnType { get; set; }
        public bool ComplexMapping { get; set; }
        public bool ValueObjectColumn { get; set; }
        public string ComplexPrefix { get; set; }
        public bool SerializedColumn { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public string ReferenceMemberName { get; set; }
        public MemberInfo MemberInfo { get; internal set; }
        public bool ExactColumnNameMatch { get; set; }
    }
}
