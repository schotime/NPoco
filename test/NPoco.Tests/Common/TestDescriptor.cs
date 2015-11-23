using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NPoco.Tests.Common
{
    public class TestDescriptor : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
#if DNXCORE50
            Console.WriteLine("Executing {0}.{1}...", test.Method.MethodInfo.DeclaringType.Name, test.Name);
#else
            Console.Write("Executing...");
#endif
        }

        public void AfterTest(ITest test)
        {
            
        }

        public ActionTargets Targets { get { return ActionTargets.Test; } }
    }
}