using NPoco.Tests.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NPoco.Tests.NewMapper.Models;

namespace NPoco.Tests.DecoratedTests.QueryTests
{
    [TestFixture]
    public class ParentChildIncludeTests : BaseDBDecoratedTest
    {
        [Test]
        public async Task TestParentChildDataIsCorrectlyPopulated()
        {
            await Database.InsertAsync(new Parent { ParentId = 1, Id = 11 });
            await Database.InsertAsync(new Parent { ParentId = 2, Id = 22 });
            await Database.InsertAsync(new Child { ChildId = 100, ParentId = 1 });
            await Database.InsertAsync(new Child { ChildId = 200, ParentId = 2 });

            var children = await Database.Query<Child>()
                .Include(c => c.Parent)
                .ToListAsync();

            Assert.AreEqual(children[0].ChildId, 100);
            Assert.AreEqual(children[0].ParentId, 1);
            Assert.AreEqual(children[0].Parent.ParentId, 1);
            Assert.AreEqual(children[0].Parent.Id, 11);
            Assert.AreEqual(children[1].ChildId, 200);
            Assert.AreEqual(children[1].ParentId, 2);
            Assert.AreEqual(children[1].Parent.ParentId, 2);
            Assert.AreEqual(children[1].Parent.Id, 22);
        }

        [Test]
        public void TestOneToOneWithIdAndRefObject()
        {
            var userDec = Database.Query<MyUserDec>()
                .Include(x => x.House)
                .Where(x => x.UserId == 2)
                .Single();

            Assert.AreEqual(userDec.UserId, 2);
            Assert.AreEqual(userDec.HouseId, 2);
            Assert.AreEqual(userDec.House.HouseId, 2);
            Assert.AreEqual(userDec.House.Address, "1 Road Street, Suburb");
        }

        [TableName("Users"), PrimaryKey("UserId")]
        public class MyUserDec
        {
            public int UserId { get; set; }
            public int HouseId { get; set; }

            [Reference(ReferenceType.OneToOne, ColumnName = "HouseId", ReferenceMemberName = "HouseId")]
            public HouseDecorated House { get; set; }
        }
    }
}
