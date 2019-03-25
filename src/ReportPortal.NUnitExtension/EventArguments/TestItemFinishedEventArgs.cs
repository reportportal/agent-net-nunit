using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, ITestReporter testReporter, string report)
        {
            Service = service;
            FinishTestItemRequest = request;
            TestReporter = testReporter;
            Report = report;
        }

        public Service Service { get; }

        public FinishTestItemRequest FinishTestItemRequest { get; }
        
        public ITestReporter TestReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
