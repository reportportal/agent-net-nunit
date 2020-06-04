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

        [Test, Ignore("this is reason")]
        public void SkippedTest()
        {

        }

        [Test]
        public void WithAttachment()
        {
            // valid file
            TestContext.AddTestAttachment("nunit_random_seed.tmp");

            // cannot read this file
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
                scope.Info("q");
            }
        }
    }
}