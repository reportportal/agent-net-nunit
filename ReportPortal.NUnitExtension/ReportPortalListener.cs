using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.NUnitExtension.Configuration;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "Report Portal extension point")]
    public partial class ReportPortalListener : ITestEventListener
    {
        static ReportPortalListener()
        {
            var configPath = Path.GetDirectoryName(new Uri(typeof(Config).Assembly.CodeBase).LocalPath) + "/ReportPortal.config.json";
            Config = Client.Converters.ModelSerializer.Deserialize<Config>(File.ReadAllText(configPath));

            Service rpService;
            if (Config.Server.Proxy != null)
            {
                rpService = new Service(Config.Server.Url, Config.Server.Project, Config.Server.Authentication.Uuid, new WebProxy(Config.Server.Proxy));
            }
            else
            {
                rpService = new Service(Config.Server.Url, Config.Server.Project, Config.Server.Authentication.Uuid);
            }

            Bridge.Service = rpService;

            _statusMap["Passed"] = Status.Passed;
            _statusMap["Failed"] = Status.Failed;
            _statusMap["Skipped"] = Status.Skipped;
            _statusMap["Inconclusive"] = Status.Skipped;
            _statusMap["Warning"] = Status.Failed;
        }

        private static Dictionary<string, Status> _statusMap = new Dictionary<string, Status>();

        private Dictionary<string, TestReporter> _suitesFlow = new Dictionary<string, TestReporter>();
        private Dictionary<string, TestReporter> _testFlowIds = new Dictionary<string, TestReporter>();
        private Dictionary<string, TestReporter> _testFlowNames = new Dictionary<string, TestReporter>();

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

                else if (xmlDoc.SelectSingleNode("/test-message") != null)
                {
                    TestMessage(xmlDoc);
                }
            }
        }
    }
}
