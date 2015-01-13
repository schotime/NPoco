using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class VersionColumnAttribute : ColumnAttribute
    {
        public VersionColumnType VersionColumnType { get; private set; }

        public VersionColumnAttribute() : this(VersionColumnType.Int) {}
        public VersionColumnAttribute(VersionColumnType versionColumnType)
        {
            VersionColumnType = versionColumnType;
        }
        public VersionColumnAttribute(string name, VersionColumnType versionColumnType) : base(name)
        {
            VersionColumnType = versionColumnType;
        }
    }

    public enum VersionColumnType
    {
        Int,
        Timestamp
    }
}