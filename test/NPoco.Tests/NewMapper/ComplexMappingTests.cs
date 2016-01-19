using System.Linq;
using NPoco;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class ComplexMappingTests
    {
        [Test]
        public void NestedClassShouldBeMappedAsAComplexObject()
        {
            var pocoData = new PocoDataFactory(new MapperCollection()).ForType(typeof(ComplexMap));

            Assert.AreEqual(7, pocoData.Columns.Count);
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("Name"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap__Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap__NestedComplexMap2__Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap__NestedComplexMap2__Name"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap2__Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap2__Name"));
        }

        [Test]
        public void NestedClassShouldBeAbleToGetValue()
        {
            var pocoData = new PocoDataFactory(new MapperCollection()).ForType(typeof(ComplexMap));
            var obj = new ComplexMap()
            {
                Name = "Bill",
                NestedComplexMap = new NestedComplexMap()
                {
                    Id = 9
                }
            };
            var val = pocoData.Columns["NestedComplexMap__Id"].GetValue(obj);
            Assert.AreEqual(9, val);
        }

        [Test]
        public void NestedNestedClassShouldBeAbleToGetValue()
        {
            var pocoData = new PocoDataFactory(new MapperCollection()).ForType(typeof(ComplexMap));
            var obj = new ComplexMap()
            {
                Name = "Bill",
                NestedComplexMap = new NestedComplexMap()
                {
                    Id = 9,
                    NestedComplexMap2 = new NestedComplexMap2()
                    {
                        Id = 18
                    }
                }
            };
            var val = pocoData.Columns["NestedComplexMap__NestedComplexMap2__Id"].GetValue(obj);
            Assert.AreEqual(18, val);
        }

    }

    public class NewMapperDecoratedTests : BaseDBDecoratedTest
    {
        [Test]
        public void Test1()
        {
            var obj = new ComplexMap()
            {
                Name = "Bill",
                NestedComplexMap = new NestedComplexMap()
                {
                    Id = 9,
                    NestedComplexMap2 = new NestedComplexMap2()
                    {
                        Id = 18
                    }
                }
            };
            Database.Insert(obj);
            var results = Database.Fetch<ComplexMap>().Single();
            Assert.AreEqual(obj.Id, results.Id);
            Assert.AreEqual(obj.Name, results.Name);
            Assert.AreEqual(obj.NestedComplexMap.Id, results.NestedComplexMap.Id);
            Assert.AreEqual(obj.NestedComplexMap.NestedComplexMap2.Id, results.NestedComplexMap.NestedComplexMap2.Id);
        }

        [Test]
        public void ProjectToComplexColumn()
        {
            var obj = new ComplexMap()
            {
                Name = "Bill",
                NestedComplexMap2 = new NestedComplexMap2()
                {
                    Id = 9,
                    Name = "Silly"
                }
            };
            Database.Insert(obj);
            var results = Database.Query<ComplexMap>().ProjectTo(x => x.NestedComplexMap2).Single();
            Assert.AreEqual(obj.NestedComplexMap2.Id, results.Id);
            Assert.AreEqual(obj.NestedComplexMap2.Name, results.Name);
        }
    }

    public class ComplexMap
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [ComplexMapping]
        public NestedComplexMap NestedComplexMap { get; set; }
        [ComplexMapping]
        public NestedComplexMap2 NestedComplexMap2 { get; set; }
    }

    public class NestedComplexMap
    {
        public int Id { get; set; }
        [ComplexMapping]
        public NestedComplexMap2 NestedComplexMap2 { get; set; }
    }

    public class NestedComplexMap2
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
