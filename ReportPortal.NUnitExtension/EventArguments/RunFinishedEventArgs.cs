using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunFinishedEventArgs: EventArgs
    {
        public RunFinishedEventArgs(Service service, FinishLaunchRequest request, LaunchReporter launchReporter)
        {
            Service = service;
            Launch = request;
            LaunchReporter = launchReporter;
        }

        public Service Service { get; }

        public FinishLaunchRequest Launch { get; }

        public LaunchReporter LaunchReporter { get; }

        public bool Canceled { get; set; }
    }
}
