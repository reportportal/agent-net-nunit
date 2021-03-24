using NUnit.Framework;
using System;
using System.IO;

namespace ReportPortal.NUnitExtension.Tests.Internal
{
    public class UnitTests1
    {
        [Test]
        [Category("")]
        [Category("smoke"), Category("smoke2")]
        [Description("desc")]
        public void PassedTest()
        {
            var categories = Shared.Context.Current.Metadata.Attributes;
            categories.Add("qwe", "abc");
        }

        [Test]
        public void FailedTest()
        {
            var categories = Shared.Context.Current.Metadata.Attributes;
            categories.Add("a", "b");
            Assert.IsTrue(false);
        }

        [Test, Ignore("this is reason")]
        public void SkippedTest()
        {

        }

        [Test]
        public void WithAttachment()
        {
            Shared.Context.Current.Log.Info("text", "image/png", new byte[] { 1, 2, 3 });

            // valid file
            File.Create("attach.tmp");
            TestContext.AddTestAttachment("attach.tmp");

            // cannot read this file
            TestContext.AddTestAttachment("ReportPortal.NUnitExtension.Tests.Internal.dll");
        }

        [Test]
        public void TestWithOutput()
        {
            TestContext.Out.WriteLine("a");

            Console.WriteLine("a");

            Shared.Context.Current.Log.Info("q");

            using (var scope = Shared.Context.Current.Log.BeginScope("s"))
            {
                scope.Info("q");
            }
        }

        [Test]
        [TestCase(1, TestName = "Abc")]
        [TestCase(2)]
        public void ParametrizedTest(int a)
        {

        }
    }
}