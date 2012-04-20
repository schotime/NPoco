using System;

namespace NPoco
{
    [AttributeUsage(AttributeTargets.Property)]
    public class VersionColumnAttribute : ColumnAttribute
    {
        public VersionColumnAttribute() {}
        public VersionColumnAttribute(string name) : base(name) { }
    }
}