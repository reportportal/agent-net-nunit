using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, TestReporter testReporter)
        {
            Service = service;
            TestItem = request;
            TestReporter = testReporter;
        }

        public Service Service { get; }

        public FinishTestItemRequest TestItem { get; }


        public TestReporter TestReporter { get; }

        public bool Canceled { get; set; }
    }
}
