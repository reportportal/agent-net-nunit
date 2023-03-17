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
        private ILaunchReporter _launchReporter;

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);
        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

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

                var eventArg = new RunStartedEventArgs(_rpService, startLaunchRequest);

                try
                {
                    BeforeRunStarted?.Invoke(this, eventArg);
                }
                catch (Exception exp)
                {
                    _traceLogger.Error("Exception was thrown in 'BeforeRunStarted' subscriber." + Environment.NewLine + exp);
                }

                if (!eventArg.Canceled)
                {
                    _launchReporter = new LaunchReporter(_rpService, Config, null, _extensionManager);

                    _launchReporter.Start(eventArg.StartLaunchRequest);

                    try
                    {
                        AfterRunStarted?.Invoke(this, new RunStartedEventArgs(_rpService, startLaunchRequest, _launchReporter, report));
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'AfterRunStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);
        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

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
                try
                {
                    BeforeRunFinished?.Invoke(this, eventArg);
                }
                catch (Exception exp)
                {
                    _traceLogger.Error("Exception was thrown in 'BeforeRunFinished' subscriber." + Environment.NewLine + exp);
                }

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

                    try
                    {
                        AfterRunFinished?.Invoke(this, new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, report));
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'AfterRunFinished' subscriber." + Environment.NewLine + exp);
                    }
                }
                else
                {
                    var sw = Stopwatch.StartNew();
                    Console.Write("Finishing to send the results to Report Portal... ");

                    _launchReporter.Sync();

                    Console.WriteLine($"Elapsed: {sw.Elapsed}");

                    var statisticsRecord = _launchReporter.StatisticsCounter.ToString();
                    _traceLogger.Info(statisticsRecord);
                    Console.WriteLine(statisticsRecord);
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
