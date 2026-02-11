using System;
using System.Collections.Generic;
using System.Text;

namespace NPoco.Tests.NewMapper.Models
{
    public class ParentAddress
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    [PrimaryKey("ParentId", AutoIncrement = false)]
    public class ParentWithComplexMapping
    {
        public int ParentId { get; set; }
        public int Id { get; set; }

        [ComplexMapping]
        public ParentAddress Address { get; set; }
    }

    [PrimaryKey("ChildId", AutoIncrement = false)]
    public class ChildWithComplexParent
    {
        public int ChildId { get; set; }
        public int ParentId { get; set; }

        [Reference(ReferenceType.OneToOne, ColumnName = "ParentId", ReferenceMemberName = "ParentId")]
        public ParentWithComplexMapping Parent { get; set; }
    }
}
