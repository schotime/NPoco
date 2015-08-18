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
        public bool? ForceUtc { get; set; }
        public MemberInfo[] MemberInfoChain { get; set; }
        public bool? IsComplexMapping { get; set; }
        public bool? IsReferenceMember { get; set; }
        public MemberInfo ReferenceMember { get; set; }
        public ReferenceMappingType? ReferenceMappingType { get; set; }
        public bool? StoredAsJson { get; set; }
        public string ComplexPrefix { get; set; }
    }
}