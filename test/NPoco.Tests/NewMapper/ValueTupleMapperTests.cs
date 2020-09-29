using System;
using System.Collections.Generic;
using System.Text;
using NPoco.Tests.Common;
using NPoco.Tests.NewMapper.Models;
using NUnit.Framework;

namespace NPoco.Tests.NewMapper
{
    public class ValueTupleMapperTests : BaseDBDecoratedTest
    {
        [Test]
        public void Test1()
        {
            var (foo, bar) = Database.Single<(string, string)>(@"select 'foo', 'bar' /*poco_dual*/");

            Assert.AreEqual(foo, "foo");
            Assert.AreEqual(bar, "bar");
        }

        [Test]
        public void Test2()
        {
            var data = Database.Single<((string foo, int i) first, (string bar, int j) second)>(@"select 'foo', 5, 'bar', 6 /*poco_dual*/");

            Assert.AreEqual(data.first.foo, "foo");
            Assert.AreEqual(data.first.i, 5);
            Assert.AreEqual(data.second.bar, "bar");
            Assert.AreEqual(data.second.j,  6);
        }

        [Test]
        public void Test3()
        {
            var (foo, bar) = Database.Single<(string, string)>(@"select 'foo', null /*poco_dual*/");

            Assert.AreEqual(foo, "foo");
            Assert.AreEqual(bar, null);
        }

        [Test]
        public void Test4()
        {
            var (foo, bar) = Database.Single<(string, TestEnum)>(@"select 'foo', 'None' /*poco_dual*/");

            Assert.AreEqual(foo, "foo");
            Assert.AreEqual(bar, TestEnum.None);
        }

        [Test]
        public void Test5()
        {
            Database.Mappers.Add(new MyMapper());

            var (foo, bar) = Database.Single<(string, MyKey)>(@"select 'foo', 77 /*poco_dual*/");

            Assert.AreEqual(foo, "foo");
            Assert.AreEqual(bar.Key, 77);
        }

        public class MyMapper : DefaultMapper
        {
            public override Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
            {
                if (destType == typeof(MyKey) && sourceType == typeof(int))
                {
                    return x => new MyKey((int) x);
                }

                return base.GetFromDbConverter(destType, sourceType);
            }
        }

        public class MyKey
        {
            public int Key { get; }

            public MyKey(int key)
            {
                Key = key;
            }
        }
    }
}
