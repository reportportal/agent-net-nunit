using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemStartedEventArgs : EventArgs
    {
        public TestItemStartedEventArgs(IClientService service, StartTestItemRequest request)
        {
            Service = service;
            StartTestItemRequest = request;
        }

        public TestItemStartedEventArgs(IClientService service, StartTestItemRequest request, ITestReporter testReporter, string report) : this(service, request)
        {
            TestReporter = testReporter;
            Report = report;
        }

        public IClientService Service { get; }

        public StartTestItemRequest StartTestItemRequest { get; }

        public ITestReporter TestReporter { get; set; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
