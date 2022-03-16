using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunStartedEventArgs : BaseNUnitEventArgs
    {
        public RunStartedEventArgs(IClientService service, StartLaunchRequest request, string report = null)
            : base(service, report)
        {
            StartLaunchRequest = request;
        }

        public RunStartedEventArgs(IClientService service, StartLaunchRequest request, ILaunchReporter launchReporter, string report)
            : this(service, request, report)
        {
            LaunchReporter = launchReporter;
        }

        public StartLaunchRequest StartLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }
    }
}
