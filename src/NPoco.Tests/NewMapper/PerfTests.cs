using System;
using System.Diagnostics;
using NPoco.RowMappers;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class PerfTests
    {
        [Test]
        public void Test11()
        {
            var fakeReader = new FakeReader();

            var pocoDataFactory = new PocoDataFactory(new MapperCollection());

            var sw = Stopwatch.StartNew();
            
            for (int j = 0; j < 1000; j++)
            {
                var newPropertyMapper = new PropertyMapper();    
                var pocoData = pocoDataFactory.ForType(typeof (NestedConvention));
                newPropertyMapper.Init(fakeReader, pocoData);
                for (int i = 0; i < 1000; i++)
                {
                    newPropertyMapper.Map(fakeReader, new RowMapperContext() { PocoData = pocoData });
                }
            }

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [Test]
        public void Test1()
        {
            var pocoData = new PocoDataFactory(new MapperCollection()).ForType(typeof(RecursionUser));

            Assert.AreEqual(4, pocoData.Members.Count);
            Assert.AreEqual("Id", pocoData.Members[0].Name);
            Assert.AreEqual("Name", pocoData.Members[1].Name);
            Assert.AreEqual("Supervisor", pocoData.Members[2].Name);
            Assert.AreEqual("CreatedBy", pocoData.Members[3].Name);
        }
    }
}