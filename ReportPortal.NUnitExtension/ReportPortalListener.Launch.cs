using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
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
                if (Config.Launch.IsDebugMode)
                {
                    launchMode = LaunchMode.Debug;
                }
                else
                {
                    launchMode = LaunchMode.Default;
                }
                var startLaunchRequest = new StartLaunchRequest
                {
                    Name = Config.Launch.Name,
                    Description = Config.Launch.Description,
                    StartTime = DateTime.UtcNow,
                    Mode = launchMode,
                    Tags = Config.Launch.Tags
                };

                var eventArg = new RunStartedEventArgs(Bridge.Service, startLaunchRequest);

                try
                {
                    if (BeforeRunStarted != null) BeforeRunStarted(this, eventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeRunStarted' subscriber." + Environment.NewLine + exp);
                }

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter = new LaunchReporter(Bridge.Service);
                    Bridge.Context.LaunchReporter.Start(eventArg.Launch);

                    try
                    {
                        if (AfterRunStarted != null) AfterRunStarted(this, new RunStartedEventArgs(Bridge.Service, startLaunchRequest, Bridge.Context.LaunchReporter));
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

                var eventArg = new RunFinishedEventArgs(Bridge.Service, finishLaunchRequest, Bridge.Context.LaunchReporter);
                try
                {
                    if (BeforeRunFinished != null) BeforeRunFinished(this, eventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeRunFinished' subscriber." + Environment.NewLine + exp);
                }

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter.Finish(finishLaunchRequest);
                    Bridge.Context.LaunchReporter.FinishTask.Wait();

                    try
                    {
                        if (AfterRunFinished != null) AfterRunFinished(this, new RunFinishedEventArgs(Bridge.Service, finishLaunchRequest, Bridge.Context.LaunchReporter));
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
