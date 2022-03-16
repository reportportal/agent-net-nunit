using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public static event EventHandler<RunStartedEventArgs> BeforeRunStarted;

        public static event EventHandler<RunStartedEventArgs> AfterRunStarted;

        public static event EventHandler<RunFinishedEventArgs> BeforeRunFinished;

        public static event EventHandler<RunFinishedEventArgs> AfterRunFinished;

        private ILaunchReporter _launchReporter;

        private void StartRun(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                LaunchMode launchMode;
                if (Config.GetValue(ConfigurationPath.LaunchDebugMode, false))
                {
                    launchMode = LaunchMode.Debug;
                }
                else
                {
                    launchMode = LaunchMode.Default;
                }
                var startLaunchRequest = new StartLaunchRequest
                {
                    Name = Config.GetValue(ConfigurationPath.LaunchName, "NUnit Launch"),
                    Description = Config.GetValue(ConfigurationPath.LaunchDescription, ""),
                    StartTime = DateTime.UtcNow,
                    Mode = launchMode,
                    Attributes = Config.GetKeyValues("Launch:Attributes", new List<KeyValuePair<string, string>>()).Select(a => new ItemAttribute { Key = a.Key, Value = a.Value }).ToList()
                };

                var runStartedEventArgs = new RunStartedEventArgs(_rpService, startLaunchRequest);

                RiseEvent(BeforeRunStarted, runStartedEventArgs, nameof(BeforeRunStarted));

                if (!runStartedEventArgs.Canceled)
                {
                    _launchReporter = new LaunchReporter(_rpService, Config, null, _extensionManager);

                    _launchReporter.Start(runStartedEventArgs.StartLaunchRequest);

                    runStartedEventArgs = new RunStartedEventArgs(_rpService, startLaunchRequest, _launchReporter, report);
                    RiseEvent(AfterRunStarted, runStartedEventArgs, nameof(AfterRunStarted));
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        private void FinishRun(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var finishLaunchRequest = new FinishLaunchRequest
                {
                    EndTime = DateTime.UtcNow,

                };

                var eventArg = new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, report);

                RiseEvent(BeforeRunFinished, eventArg, nameof(BeforeRunFinished));

                if (!eventArg.Canceled)
                {
                    var sw = Stopwatch.StartNew();
                    Console.Write("Finishing to send the results to Report Portal... ");

                    _launchReporter.Finish(finishLaunchRequest);
                    _launchReporter.Sync();

                    Console.WriteLine($"Elapsed: {sw.Elapsed}");

                    var statisticsRecord = _launchReporter.StatisticsCounter.ToString();
                    _traceLogger.Info(statisticsRecord);
                    Console.WriteLine(statisticsRecord);

                    RiseEvent(AfterRunFinished, eventArg, nameof(AfterRunFinished));
                }
            }
            catch (Exception exception)
            {
                var errorMessage = "ReportPortal exception was thrown." + Environment.NewLine + exception;
                _traceLogger.Error(errorMessage);
                Console.WriteLine(errorMessage);
            }
        }
    }
}
