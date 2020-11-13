using System;
using System.Collections.Generic;
using System.Text;

namespace NPoco.Tests.NewMapper.Models
{
    [PrimaryKey("ParentId", AutoIncrement = false)]
    public class Parent
    {
        public int ParentId { get; set; }
        public int Id { get; set; }

        public override string ToString() => $"ParentId={ParentId}, Id={Id}";
    }

    [PrimaryKey("ChildId", AutoIncrement = false)]
    public class Child
    {
        public int ChildId { get; set; }
        public int ParentId { get; set; }

        [Reference(ReferenceType.OneToOne, ColumnName = "ParentId", ReferenceMemberName = "ParentId")]
        public Parent Parent { get; set; }

        public override string ToString() => $"ChildId={ChildId}, ParentId={ParentId} ({Parent?.ToString()})";
    }
}
