using NUnit.Framework;
using System;

namespace ReportPortal.NUnitExtension.Tests.Internal.InnerFolder
{
    [SetUpFixture]
    public class ClassFixtureSetup
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            throw new Exception("Assembly SetUpFixture exception.");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            throw new Exception("Assembly TearDownFixture exception.");
        }
    }
}
