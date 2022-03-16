using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemStartedEventArgs : BaseNUnitEventArgs
    {
        public TestItemStartedEventArgs(IClientService service, StartTestItemRequest request, string report = null)
            : base(service, report)
        {
            StartTestItemRequest = request;
        }

        public TestItemStartedEventArgs(IClientService service, StartTestItemRequest request, ITestReporter testReporter, string report)
            : this(service, request, report)
        {
            TestReporter = testReporter;
        }

        public StartTestItemRequest StartTestItemRequest { get; }

        public ITestReporter TestReporter { get; set; }
    }
}
