using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
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
                    Tags = Config.GetValues(ConfigurationPath.LaunchTags, new List<string>()).ToList()
                };

                var eventArg = new RunStartedEventArgs(Bridge.Service, startLaunchRequest);

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
                    var launchId = Config.GetValue<string>("Launch:Id", "");
                    if (string.IsNullOrEmpty(launchId))
                    {
                        Bridge.Context.LaunchReporter = Bridge.Context.LaunchReporter ?? new LaunchReporter(Bridge.Service);
                    }
                    else
                    {
                        Bridge.Context.LaunchReporter = Bridge.Context.LaunchReporter ?? new LaunchReporter(Bridge.Service, launchId);
                    }

                    Bridge.Context.LaunchReporter.Start(eventArg.StartLaunchRequest);

                    try
                    {
                        AfterRunStarted?.Invoke(this, new RunStartedEventArgs(Bridge.Service, startLaunchRequest, Bridge.Context.LaunchReporter, xmlDoc.OuterXml));
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

                var eventArg = new RunFinishedEventArgs(Bridge.Service, finishLaunchRequest, Bridge.Context.LaunchReporter, xmlDoc.OuterXml);
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

                    Bridge.Context.LaunchReporter.Finish(finishLaunchRequest);
                    Bridge.Context.LaunchReporter.FinishTask.Wait();

                    Console.WriteLine($"Elapsed: {sw.Elapsed}");

                    try
                    {
                        AfterRunFinished?.Invoke(this, new RunFinishedEventArgs(Bridge.Service, finishLaunchRequest, Bridge.Context.LaunchReporter, xmlDoc.OuterXml));
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
