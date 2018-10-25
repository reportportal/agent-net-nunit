using System;
using System.Collections.Generic;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, TestReporter testReporter, Dictionary<string, string> properties)
        {
            Service = service;
            TestItem = request;
            TestReporter = testReporter;
            Properties = properties;
        }

        public Service Service { get; }

        public FinishTestItemRequest TestItem { get; }
        
        public TestReporter TestReporter { get; }

        public Dictionary<string, string> Properties { get; }

        public bool Canceled { get; set; }
    }
}
