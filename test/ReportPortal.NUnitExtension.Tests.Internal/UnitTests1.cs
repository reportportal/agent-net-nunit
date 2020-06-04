using NUnit.Framework;
using System;

namespace ReportPortal.NUnitExtension.Tests.Internal
{
    public class UnitTests1
    {
        [Test]
        [Category("smoke")]
        [Description("desc")]
        public void PassedTest()
        {
            Assert.Pass();
        }

        [Test]
        public void FailedTest()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void WithAttachment()
        {
            TestContext.AddTestAttachment("ReportPortal.NUnitExtension.Tests.Internal.dll");
        }

        [Test]
        public void TestWithOutput()
        {
            TestContext.Out.WriteLine("a");

            Console.WriteLine("a");

            Shared.Log.Info("q");

            using (var scope = Shared.Log.BeginScope("s"))
            {

            }
        }
    }
}