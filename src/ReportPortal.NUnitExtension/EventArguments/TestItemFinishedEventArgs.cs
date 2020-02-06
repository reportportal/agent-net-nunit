using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : EventArgs
    {
        public TestItemFinishedEventArgs(IClientService service, FinishTestItemRequest request, ITestReporter testReporter, string report)
        {
            Service = service;
            FinishTestItemRequest = request;
            TestReporter = testReporter;
            Report = report;
        }

        public IClientService Service { get; }

        public FinishTestItemRequest FinishTestItemRequest { get; }
        
        public ITestReporter TestReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
