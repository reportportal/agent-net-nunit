using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunFinishedEventArgs : EventArgs
    {
        public RunFinishedEventArgs(IClientService service, FinishLaunchRequest request, ILaunchReporter launchReporter, string report)
        {
            Service = service;
            FinishLaunchRequest = request;
            LaunchReporter = launchReporter;
            Report = report;
        }

        public IClientService Service { get; }

        public FinishLaunchRequest FinishLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
