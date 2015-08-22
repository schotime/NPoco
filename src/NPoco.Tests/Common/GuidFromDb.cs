using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPoco.Tests.Common
{
    [TableName("GuidFromDb"), PrimaryKey("Id", AutoIncrement = true)]
    public class GuidFromDb
    {
        public Guid Id { get; private set; }
        public string Name { get; set; }
    }
}
