using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        private ILaunchReporter _launchReporter;

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);
        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

        private void StartRun(XmlDocument xmlDoc)
        {
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
                    Console.WriteLine("Exception was thrown in 'BeforeRunStarted' subscriber." + Environment.NewLine + exp);
                }

                if (!eventArg.Canceled)
                {
                    _launchReporter = new LaunchReporter(_rpService, Config, null);

                    _launchReporter.Start(eventArg.StartLaunchRequest);

                    try
                    {
                        AfterRunStarted?.Invoke(this, new RunStartedEventArgs(_rpService, startLaunchRequest, _launchReporter, xmlDoc.OuterXml));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterRunStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);
        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

        private void FinishRun(XmlDocument xmlDoc)
        {
            try
            {
                var finishLaunchRequest = new FinishLaunchRequest
                {
                    EndTime = DateTime.UtcNow,

                };

                var eventArg = new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, xmlDoc.OuterXml);
                try
                {
                    BeforeRunFinished?.Invoke(this, eventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeRunFinished' subscriber." + Environment.NewLine + exp);
                }

                if (!eventArg.Canceled)
                {
                    var sw = Stopwatch.StartNew();
                    Console.Write("Finishing to send the results to Report Portal... ");

                    _launchReporter.Finish(finishLaunchRequest);
                    _launchReporter.Sync();

                    Console.WriteLine($"Elapsed: {sw.Elapsed}");

                    try
                    {
                        AfterRunFinished?.Invoke(this, new RunFinishedEventArgs(_rpService, finishLaunchRequest, _launchReporter, xmlDoc.OuterXml));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterRunFinished' subscriber." + Environment.NewLine + exp);
                    }

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }
    }
}
