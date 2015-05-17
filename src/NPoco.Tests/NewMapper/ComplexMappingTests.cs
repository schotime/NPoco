using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco.Tests.Common;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class ComplexMappingTests
    {
        [Test]
        public void NestedClassShouldBeMappedAsAComplexObject()
        {
            var pocoData = new PocoDataFactory((IMapper)null).ForType(typeof(ComplexMap));

            Assert.AreEqual(5, pocoData.Columns.Count);
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("Name"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap__Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap__NestedComplexMap2__Id"));
            Assert.AreEqual(true, pocoData.Columns.ContainsKey("NestedComplexMap2__Id"));
        }

        [Test]
        public void NestedClassShouldBeAbleToGetValue()
        {
            var pocoData = new PocoDataFactory((IMapper)null).ForType(typeof(ComplexMap));
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
            var pocoData = new PocoDataFactory((IMapper)null).ForType(typeof(ComplexMap));
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
    }

    public class ComplexMap
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public NestedComplexMap NestedComplexMap { get; set; }
        public NestedComplexMap2 NestedComplexMap2 { get; set; }
    }

    public class NestedComplexMap
    {
        public int Id { get; set; }
        public NestedComplexMap2 NestedComplexMap2 { get; set; }
    }

    public class NestedComplexMap2
    {
        public int Id { get; set; }
    }
}
