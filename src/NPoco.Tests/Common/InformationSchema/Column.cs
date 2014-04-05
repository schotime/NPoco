using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPoco.Tests.Common.InformationSchema
{
    [TableName("INFORMATION_SCHEMA.COLUMNS")]
    [TableAutoCreate(false)]
    class Column
    {
        [Column]
        public string TABLE_CATALOG { get; set; }

        [Column]
        public string TABLE_SCHEMA { get; set; }

        [Column]
        public string TABLE_NAME { get; set; }

        [Column]
        public string COLUMN_NAME { get; set; }

        [Column]
        public int ORDINAL_POSITION { get; set; }

        [Column]
        public string COLUMN_DEFAULT { get; set; }

        [Column]
        public string IS_NULLABLE { get; set; }

        [Column]
        public string DATA_TYPE { get; set; }

        [Column]
        public int CHARACTER_MAXIMUM_LENGTH { get; set; }

        [Column]
        public int CHARACTER_OCTET_LENGTH { get; set; }

        [Column]
        public int NUMERIC_PRECISION { get; set; }

        [Column]
        public int NUMERIC_PRECISION_RADIX { get; set; }

        [Column]
        public int NUMERIC_SCALE { get; set; }

        [Column]
        public int DATETIME_PRECISION { get; set; }

        [Column]
        public string CHARACTER_SET_CATALOG { get; set; }

        [Column]
        public string CHARACTER_SET_SCHEMA { get; set; }

        [Column]
        public string CHARACTER_SET_NAME { get; set; }

        [Column]
        public string COLLATION_CATALOG { get; set; }

        [Column]
        public string COLLATION_SCHEMA { get; set; }

        [Column]
        public string COLLATION_NAME { get; set; }

        [Column]
        public string DOMAIN_CATALOG { get; set; }

        [Column]
        public string DOMAIN_SCHEMA { get; set; }

        [Column]
        public string DOMAIN_NAME { get; set; }
    }

}