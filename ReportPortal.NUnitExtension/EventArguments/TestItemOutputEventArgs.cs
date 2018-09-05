using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemOutputEventArgs : EventArgs
    {
        public TestItemOutputEventArgs(Service service, AddLogItemRequest request, TestReporter testReporter)
        {
            Service = service;
            LogRequest = request;
            TestReporter = testReporter;
        }

        public Service Service { get; }

        public AddLogItemRequest LogRequest { get; }

        public TestReporter TestReporter { get; }

        public bool Canceled { get; set; }
    }
}
