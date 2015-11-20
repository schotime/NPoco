using System;
using NPoco;

namespace NPoco.Tests.NewMapper.Models
{
    public class UserWithAddress
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [SerializedColumn]
        public MyAddress Address { get; set; }

        public class MyAddress
        {
            public int StreetNo { get; set; }
            public string StreetName { get; set; }
            public DateTime MovedInOn { get; set; }
            public AddressInfo AddressFurtherInfo { get; set; }

            public class AddressInfo
            {
                public string PostCode { get; set; }
            }
        }
    }
}