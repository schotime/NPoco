using NPoco;

namespace NPoco.Tests.Common
{
    [TestDescriptor]
    public class BaseDBTest
    {
        public IDatabase Database { get; set; }
        public TestDatabase TestDatabase { get; set; }
    }
}
