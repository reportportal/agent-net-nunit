using NUnit.Framework;
using System;
using System.IO;

namespace ReportPortal.NUnitExtension.Tests.Internal
{
    public class UnitTests1
    {
        [Test]
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
            Shared.Log.Message(new Client.Abstractions.Requests.CreateLogItemRequest
            {
                Time = DateTime.UtcNow,
                Level = Client.Abstractions.Models.LogLevel.Info,
                Text = "text",
                Attach = new Client.Abstractions.Responses.Attach
                {
                    Data = new byte[] { 1, 2, 3 },
                    MimeType = "image/png",
                    Name = "file.name"
                }
            });

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

            Shared.Log.Info("q");

            using (var scope = Shared.Log.BeginScope("s"))
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