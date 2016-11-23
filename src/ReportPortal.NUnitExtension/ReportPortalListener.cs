using Newtonsoft.Json;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.NUnitExtension.Configuration;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "Report Portal extension point")]
    public partial class ReportPortalListener : ITestEventListener
    {
        static ReportPortalListener()
        {
            var configPath = Path.GetDirectoryName(new Uri(typeof(Config).Assembly.CodeBase).LocalPath) + "\\ReportPortal.conf";
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

            var rpService = new Service(Config.Server.Url, Config.Server.Project, Config.Server.Authentication.Uuid);
            Bridge.Service = rpService;

            _statusMap["Passed"] = Status.Passed;
            _statusMap["Failed"] = Status.Failed;
            _statusMap["Skipped"] = Status.Skipped;
            _statusMap["Inconclusive"] = Status.Skipped;
        }

        private static Dictionary<string, Status> _statusMap = new Dictionary<string, Status>();

        private Dictionary<string, TestItemStartedEventArgs> _suitesFlow = new Dictionary<string, TestItemStartedEventArgs>();
        private Dictionary<string, TestItemStartedEventArgs> _testFlowIds = new Dictionary<string, TestItemStartedEventArgs>();
        private Dictionary<string, string> _testFlowNames = new Dictionary<string, string>();

        public static Config Config { get; private set; }

        public void OnTestEvent(string report)
        {
            if (Config.IsEnabled)
            {
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
                else if (xmlDoc.SelectSingleNode("/start-suite") != null)
                {
                    StartSuite(xmlDoc);
                }
                else if (xmlDoc.SelectSingleNode("/test-suite") != null)
                {
                    FinishSuite(xmlDoc);
                }
                else if (xmlDoc.SelectSingleNode("/start-test") != null)
                {
                    StartTest(xmlDoc);
                }
                else if (xmlDoc.SelectSingleNode("/test-case") != null)
                {
                    FinishTest(xmlDoc);
                }
                else if (xmlDoc.SelectSingleNode("/test-output") != null)
                {
                    TestOutput(xmlDoc);
                }
            }
        }
    }
}
