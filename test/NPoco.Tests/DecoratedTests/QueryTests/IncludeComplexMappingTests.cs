using NPoco.Tests.Common;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class IncludeComplexMappingTests : BaseDBDecoratedTest
    {
        [Test]
        public async Task IncludeShouldPopulateComplexMappingOnJoinedEntity()
        {
            await Database.InsertAsync(new ParentWithComplexMapping
            {
                ParentId = 1,
                Id = 10,
                Address = new ParentAddress { Street = "Main St", City = "Springfield" }
            });
            await Database.InsertAsync(new ChildWithComplexParent { ChildId = 100, ParentId = 1 });

            var children = await Database.Query<ChildWithComplexParent>()
                .Include(c => c.Parent)
                .ToListAsync();

            Assert.AreEqual(1, children.Count);
            Assert.AreEqual(100, children[0].ChildId);
            Assert.AreEqual(1, children[0].ParentId);
            Assert.AreEqual(1, children[0].Parent.ParentId);
            Assert.AreEqual(10, children[0].Parent.Id);
            Assert.IsNotNull(children[0].Parent.Address);
            Assert.AreEqual("Main St", children[0].Parent.Address.Street);
            Assert.AreEqual("Springfield", children[0].Parent.Address.City);
        }

        [Test]
        public async Task IncludeShouldPopulateComplexMappingOnMultipleJoinedRows()
        {
            await Database.InsertAsync(new ParentWithComplexMapping
            {
                ParentId = 1,
                Id = 10,
                Address = new ParentAddress { Street = "First Ave", City = "CityA" }
            });
            await Database.InsertAsync(new ParentWithComplexMapping
            {
                ParentId = 2,
                Id = 20,
                Address = new ParentAddress { Street = "Second Ave", City = "CityB" }
            });
            await Database.InsertAsync(new ChildWithComplexParent { ChildId = 100, ParentId = 1 });
            await Database.InsertAsync(new ChildWithComplexParent { ChildId = 200, ParentId = 2 });

            var children = await Database.Query<ChildWithComplexParent>()
                .Include(c => c.Parent)
                .ToListAsync();

            Assert.AreEqual(2, children.Count);

            Assert.AreEqual(1, children[0].Parent.ParentId);
            Assert.AreEqual("First Ave", children[0].Parent.Address.Street);
            Assert.AreEqual("CityA", children[0].Parent.Address.City);

            Assert.AreEqual(2, children[1].Parent.ParentId);
            Assert.AreEqual("Second Ave", children[1].Parent.Address.Street);
            Assert.AreEqual("CityB", children[1].Parent.Address.City);
        }
    }
}
