using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunFinishedEventArgs : BaseNUnitEventArgs
    {
        public RunFinishedEventArgs(IClientService service, FinishLaunchRequest request, ILaunchReporter launchReporter, string report)
            : base(service, report)
        {
            FinishLaunchRequest = request;
            LaunchReporter = launchReporter;
        }

        public FinishLaunchRequest FinishLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }
    }
}
