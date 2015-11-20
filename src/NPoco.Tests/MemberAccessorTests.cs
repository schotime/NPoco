using System;
using NPoco;
using NUnit.Framework;

namespace NPoco.Tests
{
    public class MemberAccessorTests
    {
        [Test]
        public void CanReadProp()
        {
            var accessor = new MemberAccessor(typeof (MemberAccessorTestClass), "NameProp");
            var name = Guid.NewGuid().ToString();
            var result = accessor.Get(new MemberAccessorTestClass() {NameProp = name});
            Assert.AreEqual(name, result);
        }

        [Test]
        public void CanReadField()
        {
            var accessor = new MemberAccessor(typeof(MemberAccessorTestClass), "NameField");
            var name = Guid.NewGuid().ToString();
            var result = accessor.Get(new MemberAccessorTestClass() { NameField = name });
            Assert.AreEqual(name, result);
        }
        
        [Test]
        public void CanWriteProp()
        {
            var accessor = new MemberAccessor(typeof (MemberAccessorTestClass), "NameProp");
            var name = Guid.NewGuid().ToString();
            var memberAccessorTestClass = new MemberAccessorTestClass();
            accessor.Set(memberAccessorTestClass, name);
            Assert.AreEqual(name, memberAccessorTestClass.NameProp);
        }

        [Test]
        public void CanWriteField()
        {
            var accessor = new MemberAccessor(typeof(MemberAccessorTestClass), "NameField");
            var name = Guid.NewGuid().ToString();
            var memberAccessorTestClass = new MemberAccessorTestClass();
            accessor.Set(memberAccessorTestClass, name);
            Assert.AreEqual(name, memberAccessorTestClass.NameField);
        }
    }

    public class MemberAccessorTestClass
    {
        public string NameProp { get; set; }
        public string NameField;
    }
}
