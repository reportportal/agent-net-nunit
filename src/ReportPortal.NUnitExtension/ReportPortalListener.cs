using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "ReportPortal extension to send test results")]
    public partial class ReportPortalListener : ITestEventListener
    {
        static ReportPortalListener()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Microsoft Test Host references to v4 of this assembly for .net framework, but RP needs v6
            if (args.Name.StartsWith("System.Runtime.CompilerServices.Unsafe", StringComparison.OrdinalIgnoreCase))
            {
                return Assembly.Load("System.Runtime.CompilerServices.Unsafe");
            }

            return null;
        }

        private readonly ITraceLogger _traceLogger;

        private Client.Abstractions.IClientService _rpService;

        private IExtensionManager _extensionManager = new ExtensionManager();

        public ReportPortalListener()
        {
            var baseDir = Path.GetDirectoryName(new Uri(typeof(ReportPortalListener).Assembly.CodeBase).LocalPath);

            // first invocation of internal logger so setting base dir
            _traceLogger = TraceLogManager.Instance.WithBaseDir(baseDir).GetLogger(typeof(ReportPortalListener));

            Config = new ConfigurationBuilder().AddDefaults(baseDir).Build();

            _rpService = new Shared.Reporter.Http.ClientServiceBuilder(Config).Build();

            _extensionManager.Explore(baseDir);

            Shared.Extensibility.Embedded.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-nunit");

            _statusMap["Passed"] = Status.Passed;
            _statusMap["Failed"] = Status.Failed;
            _statusMap["Skipped"] = Status.Skipped;
            _statusMap["Inconclusive"] = Status.Skipped;
            _statusMap["Warning"] = Status.Failed;
        }

        private static Dictionary<string, Status> _statusMap = new Dictionary<string, Status>();

        private Dictionary<string, FlowItemInfo> _flowItems = new Dictionary<string, FlowItemInfo>();

        public static IConfiguration Config { get; private set; }

        public void OnTestEvent(string report)
        {
            _traceLogger.Verbose($"Agent got an event:{Environment.NewLine}{report}");

            if (Config.GetValue("Enabled", true))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(report);

                if (report.StartsWith("<start-run"))
                {
                    StartRun(report);
                }
                else if (report.StartsWith("<test-run"))
                {
                    FinishRun(report);
                }
                else if (report.StartsWith("<start-suite"))
                {
                    StartSuite(report);
                }
                else if (report.StartsWith("<test-suite"))
                {
                    FinishSuite(report);
                }
                else if (report.StartsWith("<start-test"))
                {
                    StartTest(report);
                }
                else if (report.StartsWith("<test-case"))
                {
                    FinishTest(report);
                }
                else if (report.StartsWith("<test-output"))
                {
                    TestOutput(report);
                }

                else if (report.StartsWith("<test-message"))
                {
                    TestMessage(report);
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

            public override string ToString()
            {
                return $"{FullName}";
            }
        }
    }
}
