using NUnit.Framework;
using System;

namespace ReportPortal.NUnitExtension.Tests.Internal
{
    [Description("All tests should be failed")]
    public class UnitTests2
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            throw new Exception();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}