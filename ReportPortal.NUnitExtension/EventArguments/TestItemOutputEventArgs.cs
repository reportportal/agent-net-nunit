using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemOutputEventArgs : EventArgs
    {
        public TestItemOutputEventArgs(Service service, AddLogItemRequest request, ITestReporter testReporter)
        {
            Service = service;
            AddLogItemRequest = request;
            TestReporter = testReporter;
        }

        public Service Service { get; }

        public AddLogItemRequest AddLogItemRequest { get; }

        public ITestReporter TestReporter { get; }

        public bool Canceled { get; set; }
    }
}
