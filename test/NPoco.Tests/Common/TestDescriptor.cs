using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NPoco.Tests.Common
{
    public class TestDescriptor : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
            Console.Write("Executing...");
        }

        public void AfterTest(ITest test)
        {
            
        }

        public ActionTargets Targets { get { return ActionTargets.Test; } }
    }
}