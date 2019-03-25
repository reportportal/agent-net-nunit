using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemStartedEventArgs : EventArgs
    {
        public TestItemStartedEventArgs(Service service, StartTestItemRequest request)
        {
            Service = service;
            StartTestItemRequest = request;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, ITestReporter testReporter, string report) : this(service, request)
        {
            TestReporter = testReporter;
            Report = report;
        }

        public Service Service { get; }

        public StartTestItemRequest StartTestItemRequest { get; }

        public ITestReporter TestReporter { get; set; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
