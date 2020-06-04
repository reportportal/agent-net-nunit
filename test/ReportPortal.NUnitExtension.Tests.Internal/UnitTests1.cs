using NUnit.Framework;
using System;

namespace ReportPortal.NUnitExtension.Tests.Internal
{
    public class UnitTests1
    {
        [Test]
        [Category("smoke")]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public void TestWithOutput()
        {
            Console.WriteLine("a");

            ReportPortal.Shared.Log.Info("q");
        }
    }
}