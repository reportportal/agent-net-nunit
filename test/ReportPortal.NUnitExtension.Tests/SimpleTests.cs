using FluentAssertions;
using NUnit.Engine;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using WireMock.Server;

namespace ReportPortal.NUnitExtension.Tests
{
    public class SimpleTests
    {
        WireMockServer _server;

        TestPackage _testPackage = new TestPackage("ReportPortal.NUnitExtension.Tests.Internal.dll");

        [SetUp]
        public void Setup()
        {
            _server = WireMockServer.Start();

            Environment.SetEnvironmentVariable("ReportPortal_Server_Url", $"http://localhost:{_server.Ports[0]}/api/v1");
            Environment.SetEnvironmentVariable("ReportPortal_Server_Project", "any_project");
            Environment.SetEnvironmentVariable("ReportPortal_Server_Authentication_Uuid", "any_token");
        }

        [TearDown]
        public void TearDown()
        {
            _server.Stop();
        }

        [Test]
        public void Test1()
        {
            var listener = new ReportPortalListener();

            var testRunner = TestEngineActivator.CreateInstance().GetRunner(_testPackage);

            testRunner.Run(listener, TestFilter.Empty);

            _server.LogEntries.Count().Should().BeGreaterThan(0);
        }
    }
}