using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunFinishedEventArgs: EventArgs
    {
        public RunFinishedEventArgs(Service service, FinishLaunchRequest request, ILaunchReporter launchReporter)
        {
            Service = service;
            FinishLaunchRequest = request;
            LaunchReporter = launchReporter;
        }

        public Service Service { get; }

        public FinishLaunchRequest FinishLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }

        public bool Canceled { get; set; }
    }
}
