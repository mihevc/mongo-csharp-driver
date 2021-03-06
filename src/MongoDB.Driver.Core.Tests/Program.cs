﻿using System;
using System.Reflection;
using NUnit.Common;
using NUnitLite;

namespace MongoDB.Driver.Core.Tests
{
    public class Program
    {
        public static int Main(string[] args)
        {
#if DNX451
        return new AutoRun().Execute(args);
#else
            var ar = new AutoRun(typeof(Program).GetTypeInfo().Assembly).Execute(args, new ExtendedTextWrapper(Console.Out), Console.In);
            Console.ReadLine();
            return ar;
#endif
        }
    }
}
