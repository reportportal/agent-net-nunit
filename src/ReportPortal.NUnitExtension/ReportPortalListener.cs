using NUnit.Engine;
using NUnit.Engine.Extensibility;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Attributes;
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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension
{
    [Extension(Description = "ReportPortal extension to send test results")]
    public partial class ReportPortalListener : ITestEventListener
    {
        private static readonly Regex _reportKeyRegex = new Regex(@"^(<[^\s>]*)");

        private static readonly IPropertyHandler[] _propertyHandlers;

        private static readonly IAttributeHandler[] _attributeHandlers;

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

        static ReportPortalListener()
        {
            _propertyHandlers = ScanAssemblyForHandlers<IPropertyHandler>();
            _attributeHandlers = ScanAssemblyForHandlers<IAttributeHandler>();
        }

        public ReportPortalListener()
        {
            _processors = ScanTypeForProcessors();
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

            if (Config.IsEnabled() && _processors.TryGetValue(GetReportKey(report), out var processor))
            {
                processor.Invoke(report);
            }
        }

        private static THandler[] ScanAssemblyForHandlers<THandler>()
        {
            return typeof(ReportPortalListener).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.HasDefaultConstructor() && typeof(THandler).IsAssignableFrom(t))
                .Select(t => (THandler)Activator.CreateInstance(t))
                .ToArray();
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

        private static string GetReportKey(string report)
        {
            return _reportKeyRegex.Match(report).Value;
        }

        private Dictionary<string, Action<string>> ScanTypeForProcessors()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var methods = typeof(ReportPortalListener).GetMethods(flags)
                 .Where(method => method.GetCustomAttribute<ReportKeyAttribute>() != null)
                 .Select(method => new { Method = method, method.GetCustomAttribute<ReportKeyAttribute>().Key })
                 .ToList();

            return methods.ToDictionary(
                kvp => kvp.Key,
                kvp => (Action<string>)kvp.Method.CreateDelegate(typeof(Action<string>), this));
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
