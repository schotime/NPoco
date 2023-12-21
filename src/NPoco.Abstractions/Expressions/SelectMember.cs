using System;

namespace NPoco.Expressions
{
    public class SelectMember : IEquatable<SelectMember>
    {
        public Type EntityType { get; set; }
        public string SelectSql { get; set; }
        public PocoColumn PocoColumn { get; set; }
        public PocoColumn[] PocoColumns { get; set; }

        public bool Equals(SelectMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(EntityType, other.EntityType) && Equals(PocoColumn, other.PocoColumn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SelectMember)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((EntityType != null ? EntityType.GetHashCode() : 0) * 397) ^ (PocoColumn != null ? PocoColumn.GetHashCode() : 0);
            }
        }
    }
}
