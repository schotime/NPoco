using System;

namespace NPoco.Tests.Common
{
    [TableName("CompositeObjects")]
    [PrimaryKey("Key1ID", AutoIncrement = false)]
    [ExplicitColumns]
    public class AssignedPkObjectDecorated
    {
        [Column("Key1ID")]
        public int Key1ID { get; set; }

        [Column("Key2ID")]
        public int Key2ID { get; set; }

        [Column("Key3ID")]
        public int Key3ID { get; set; }

        [Column("TextData")]
        public string TextData { get; set; }

        [Column("DateEntered")]
        public DateTime DateEntered { get; set; }

        [Column("DateUpdated")]
        public DateTime? DateUpdated { get; set; }
    }
}