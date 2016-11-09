using Newtonsoft.Json;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.Configuration;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.IO;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "Report Portal extension point")]
    public class ReportPortalListener : ITestEventListener
    {
        static ReportPortalListener()
        {
            var configPath = Path.GetDirectoryName(new Uri(typeof(Config).Assembly.CodeBase).LocalPath) + "\\ReportPortal.conf";
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

            var rpService = new Service(Config.Server.Url, Config.Server.Project, Config.Server.Authentication.Uuid);
            Bridge.Service = rpService;
        }

        public static Config Config { get; private set; }

        public void OnTestEvent(string report)
        {
            Console.WriteLine(report + Environment.NewLine);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(report);
            if (xmlDoc.SelectSingleNode("/start-run") != null)
            {
                StartRun(xmlDoc);
            }
            else if (xmlDoc.SelectSingleNode("/test-run") != null)
            {
                FinishRun(xmlDoc);
            }
        }

        private string _launchId;

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);
        public static event RunStartedHandler BeforeRunStarted;

        private void StartRun(XmlDocument xmlDoc)
        {
            LaunchMode launchMode;
            if (Config.Launch.IsDebugMode == true)
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
            if (BeforeRunStarted != null) BeforeRunStarted(this, eventArg);

            _launchId = Bridge.Service.StartLaunch(startLaunchRequest).Id;
        }

        private void FinishRun(XmlDocument xmlDoc)
        {
            var finishLaunchRequest = new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            };
            Bridge.Service.FinishLaunch(_launchId, finishLaunchRequest);
        }
    }
}
