using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemOutputEventArgs : BaseNUnitEventArgs
    {
        public TestItemOutputEventArgs(IClientService service, CreateLogItemRequest request, ITestReporter testReporter, string report)
            : base(service, report)
        {
            AddLogItemRequest = request;
            TestReporter = testReporter;
        }

        public CreateLogItemRequest AddLogItemRequest { get; }

        public ITestReporter TestReporter { get; }
    }
}
