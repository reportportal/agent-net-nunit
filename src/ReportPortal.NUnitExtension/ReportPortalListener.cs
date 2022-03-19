using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.NUnitExtension.Handlers.Attributes;
using ReportPortal.NUnitExtension.Handlers.Properties;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "ReportPortal extension to send test results")]
    public partial class ReportPortalListener : ITestEventListener
    {
        private static readonly IPropertyHandler[] _propertyHandlers = new IPropertyHandler[]
        {
            new AuthorPropertyHandler(),
            new DescriptionPropertyHandler(),
            new CategoryPropertyHandler(),
            new RetryPropertyHandler(),
        };

        private static readonly IAttributeHandler[] _attributeHandlers = new IAttributeHandler[]
        {
            new AttachmentsHandler(),
            new FailureHandler(),
            new ReasonHandler(),
        };

        private static readonly Dictionary<string, Status> _statusMap = new Dictionary<string, Status>
        {
            ["Passed"] = Status.Passed,
            ["Failed"] = Status.Failed,
            ["Skipped"] = Status.Skipped,
            ["Inconclusive"] = Status.Skipped,
            ["Warning"] = Status.Failed,
        };

        private readonly ITraceLogger _traceLogger;

        private readonly Client.Abstractions.IClientService _rpService;

        private readonly IExtensionManager _extensionManager = new ExtensionManager();

        private readonly Dictionary<string, Action<string>> _processors;

        private readonly Dictionary<string, FlowItemInfo> _flowItems = new Dictionary<string, FlowItemInfo>();

        public ReportPortalListener()
        {
            _processors = GetProcessors();
            var baseDir = Path.GetDirectoryName(new Uri(typeof(ReportPortalListener).Assembly.CodeBase).LocalPath);

            // first invocation of internal logger so setting base dir
            _traceLogger = TraceLogManager.Instance.WithBaseDir(baseDir).GetLogger(typeof(ReportPortalListener));

            Config = new ConfigurationBuilder().AddDefaults(baseDir).Build();

            _rpService = new Shared.Reporter.Http.ClientServiceBuilder(Config).Build();
            _extensionManager.Explore(baseDir);

            Shared.Extensibility.Embedded.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-nunit");
        }

        public static IConfiguration Config { get; private set; }

        public void OnTestEvent(string report)
        {
            _traceLogger.Verbose($"Agent got an event:{Environment.NewLine}{report}");

            if (Config.IsEnabled())
            {
                Process(report);
            }
        }

        private static bool IsShouldBeDeferred(XElement xElement)
        {
            return xElement.Attribute("site")?.Value == "Parent";
        }

        private static void HandleProperties(XElement xElement, FinishTestItemRequest request)
        {
            for (int index = 0; index < _propertyHandlers.Length; index++)
            {
                _propertyHandlers[index].Handle(xElement, request);
            }
        }

        private static void HandleAttributes(XElement xElement, ITestReporter reporter)
        {
            for (int index = 0; index < _attributeHandlers.Length; index++)
            {
                _attributeHandlers[index].Handle(xElement, reporter);
            }
        }

        private Dictionary<string, Action<string>> GetProcessors()
        {
            return new Dictionary<string, Action<string>>
            {
                ["<start-run"] = StartRun,
                ["<test-run"] = FinishRun,
                ["<start-test"] = StartTest,
                ["<test-case"] = FinishTest,
                ["<start-suite"] = StartSuite,
                ["<test-suite"] = FinishSuite,
                ["<test-output"] = TestOutput,
                ["<test-message"] = TestMessage
            };
        }

        private void Process(string report)
        {
            var key = _processors.Keys.FirstOrDefault(k => report.StartsWith(k));

            if (_processors.TryGetValue(key, out var processor))
            {
                processor(report);
            }
        }

        private void RiseEvent<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs args, string subscriber)
            where TEventArgs : EventArgs
        {
            InvokeSafely(() => handler?.Invoke(this, args), $"Exception was thrown in '{subscriber}' subscriber.");
        }

        private void InvokeSafely(Action action, string errorMessage = "ReportPortal exception was thrown.")
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _traceLogger.Error(string.Concat(errorMessage, Environment.NewLine, exception));
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
