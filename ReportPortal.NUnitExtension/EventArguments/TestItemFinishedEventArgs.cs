using System;
using System.Collections.Generic;
using System.Xml;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, TestReporter testReporter, string report)
        {
            Service = service;
            TestItem = request;
            TestReporter = testReporter;
            Report = report;
        }

        public Service Service { get; }

        public FinishTestItemRequest TestItem { get; }
        
        public TestReporter TestReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
