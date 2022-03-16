using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemFinishedEventArgs : BaseNUnitEventArgs
    {
        public TestItemFinishedEventArgs(IClientService service, FinishTestItemRequest request, ITestReporter testReporter, string report)
            : base(service, report)
        {
            FinishTestItemRequest = request;
            TestReporter = testReporter;
        }

        public FinishTestItemRequest FinishTestItemRequest { get; }

        public ITestReporter TestReporter { get; }
    }
}
