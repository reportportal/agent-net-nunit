using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemOutputEventArgs : EventArgs
    {
        public TestItemOutputEventArgs(IClientService service, CreateLogItemRequest request, ITestReporter testReporter, string report)
        {
            Service = service;
            AddLogItemRequest = request;
            TestReporter = testReporter;
            Report = report;
        }

        public IClientService Service { get; }

        public CreateLogItemRequest AddLogItemRequest { get; }

        public ITestReporter TestReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
