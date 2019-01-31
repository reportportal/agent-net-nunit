using NUnit.Engine;
using NUnit.Engine.Extensibility;
using NUnit.Framework.Interfaces;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.Shared.Reporter;
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
            var jsonPath = Path.GetDirectoryName(new Uri(typeof(ReportPortalListener).Assembly.CodeBase).LocalPath) + "/ReportPortal.config.json";
            Config = new ConfigurationBuilder().AddJsonFile(jsonPath).AddEnvironmentVariables().Build();

            Service rpService;

            var uri = Config.GetValue<string>(ConfigurationPath.ServerUrl);
            var project = Config.GetValue<string>(ConfigurationPath.ServerProject);
            var uuid = Config.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid);

            var proxyServer = Config.GetValue<string>("Server:Proxy", null);
            if (!string.IsNullOrEmpty(proxyServer))
            {
                rpService = new Service(new Uri(uri), project, uuid, new WebProxy(proxyServer));
            }
            else
            {
                rpService = new Service(new Uri(uri), project, uuid);
            }

            Bridge.Service = rpService;

            _statusMap[TestStatus.Passed] = Status.Passed;
            _statusMap[TestStatus.Failed] = Status.Failed;
            _statusMap[TestStatus.Skipped] = Status.Skipped;
            _statusMap[TestStatus.Inconclusive] = Status.Skipped;
            _statusMap[TestStatus.Warning] = Status.Failed;
        }

        private static Dictionary<TestStatus, Status> _statusMap = new Dictionary<TestStatus, Status>();

        private Dictionary<string, FlowItemInfo> _flowItems = new Dictionary<string, FlowItemInfo>();

        public static IConfiguration Config { get; private set; }

        public void OnTestEvent(string report)
        {
            if (Config.GetValue("Enabled", true))
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

        internal class FlowItemInfo
        {
            public FlowItemInfo(string id, string parentId, FlowType flowType, string fullName, ITestReporter reporter, DateTime startTime)
            {
                Id = id;
                ParentId = parentId;
                FlowItemType = flowType;
                FullName = fullName;
                TestReporter = reporter;
                StartTime = startTime;
            }

            public string Id { get; }
            public string ParentId { get; }

            public FlowType FlowItemType { get; }

            public string FullName { get; }

            public ITestReporter TestReporter { get; }

            public DateTime StartTime { get; }

            internal enum FlowType
            {
                Suite,
                Test
            }

            public FinishTestItemRequest FinishTestItemRequest { get; set; }

            public string Report { get; set; }

            public Action<string, FinishTestItemRequest, string, string> DeferredFinishAction { get; set; }
        }
    }
}
