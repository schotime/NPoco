using System;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class MapperTestsForDecorated : BaseDBDecoratedTest
    {
        public class TestMapper : DefaultMapper
        {
            public bool IsGetFromDbConverter { get; set; }

            public override Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
            {
                IsGetFromDbConverter = true;
                return base.GetFromDbConverter(destType, sourceType);
            }
        }

        [Test]
        public void AssertThatMapperIsCalledWhenMapperSetOnDatabase()
        {
            var mapper = new TestMapper();
            Database.Mapper = mapper;
            var users = Database.Fetch<UserFieldDecorated>();
            Assert.AreEqual(true, mapper.IsGetFromDbConverter);
        }
    }
}