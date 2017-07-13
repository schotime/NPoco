using System;
using System.Reflection;

namespace NPoco.FluentMappings
{
    public class ColumnDefinition
    {
        public MemberInfo MemberInfo { get; set; }
        public string DbColumnName { get; set; }
        public string DbColumnAlias { get; set; }
        public Type DbColumnType { get; set; }
        public bool? ResultColumn { get; set; }
        public bool? IgnoreColumn { get; set; }
        public bool? VersionColumn { get; set; }
        public VersionColumnType? VersionColumnType { get; set; }
        public bool? ComputedColumn { get; set; }
        public ComputedColumnType? ComputedColumnType { get; set; }
        public bool? ForceUtc { get; set; }
        public MemberInfo[] MemberInfoChain { get; set; }
        public bool? IsComplexMapping { get; set; }
        public bool? IsReferenceMember { get; set; }
        public MemberInfo ReferenceMember { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public bool? Serialized { get; set; }
        public string ComplexPrefix { get; set; }
        public bool? ValueObjectColumn { get; set; }
        public string ValueObjectColumnName { get; set; }
    }
}