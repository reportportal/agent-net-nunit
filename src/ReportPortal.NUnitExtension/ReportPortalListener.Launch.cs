using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Attributes;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.Shared.Reporter;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        private const string DefaultName = "NUnit Launch";

        private const string DefaultDescription = "";

        public static event EventHandler<RunStartedEventArgs> BeforeRunStarted;

        public static event EventHandler<RunStartedEventArgs> AfterRunStarted;

        public static event EventHandler<RunFinishedEventArgs> BeforeRunFinished;

        public static event EventHandler<RunFinishedEventArgs> AfterRunFinished;

        private ILaunchReporter _launchReporter;

        [ReportKey("<start-run")]
        private void StartRun(string report) => InvokeSafely(() => StartLaunch(report));

        private void StartLaunch(string report)
        {
            var startLaunchRequest = new StartLaunchRequest
            {
                StartTime = DateTime.UtcNow,
                Mode = Config.GetMode(),
                Attributes = Config.GetAttributes(),
                Name = Config.GetName(DefaultName),
                Description = Config.GetDescription(DefaultDescription),
            };

            var runStartedEventArgs = new RunStartedEventArgs(_rpService, startLaunchRequest);
            RiseEvent(BeforeRunStarted, runStartedEventArgs, nameof(BeforeRunStarted));

            if (runStartedEventArgs.Canceled)
            {
                return;
            }

            _launchReporter = new LaunchReporter(_rpService, Config, null, _extensionManager);
            _launchReporter.Start(runStartedEventArgs.StartLaunchRequest);

            runStartedEventArgs = new RunStartedEventArgs(_rpService, startLaunchRequest, _launchReporter, report);
            RiseEvent(AfterRunStarted, runStartedEventArgs, nameof(AfterRunStarted));
        }

        [ReportKey("<test-run")]
        private void FinishRun(string report) => InvokeSafely(() => FinishLaunch(report));

        private void FinishLaunch(string report)
        {
            var finishLaunchRequest = new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow,
            };

            var runFinishedEventArgs = new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, report);
            RiseEvent(BeforeRunFinished, runFinishedEventArgs, nameof(BeforeRunFinished));

            if (runFinishedEventArgs.Canceled)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            Console.Write("Finishing to send the results to Report Portal...");

            _launchReporter.Finish(finishLaunchRequest);
            _launchReporter.Sync();

            Console.WriteLine($"Elapsed: {sw.Elapsed}");

            var statisticsRecord = _launchReporter.StatisticsCounter.ToString();
            _traceLogger.Info(statisticsRecord);
            Console.WriteLine(statisticsRecord);

            runFinishedEventArgs = new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, report);
            RiseEvent(AfterRunFinished, runFinishedEventArgs, nameof(AfterRunFinished));
        }
    }
}
