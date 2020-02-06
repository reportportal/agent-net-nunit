using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunStartedEventArgs : EventArgs
    {
        public RunStartedEventArgs(IClientService service, StartLaunchRequest request)
        {
            Service = service;
            StartLaunchRequest = request;
        }

        public RunStartedEventArgs(IClientService service, StartLaunchRequest request, ILaunchReporter launchReporter, string report) : this(service, request)
        {
            LaunchReporter = launchReporter;
            Report = report;
        }

        public IClientService Service { get; }

        public StartLaunchRequest StartLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
