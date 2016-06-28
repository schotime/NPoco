using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnitLite;
using NUnit.Common;
using NUnit.Framework;
using System.Reflection;

namespace NPoco.Tests
{
    public class Program
    {
        public static int Main(string[] args)
        {
#if NET452
            return new AutoRun().Execute(args);
#else
            return new AutoRun(typeof(Program).GetTypeInfo().Assembly)
                .Execute(args, new ExtendedTextWrapper(Console.Out), Console.In);
#endif
        }
    }
}
