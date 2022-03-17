using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.Shared.Reporter;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public static EventHandler<TestItemStartedEventArgs> BeforeSuiteStarted;

        public static EventHandler<TestItemStartedEventArgs> AfterSuiteStarted;

        public static EventHandler<TestItemFinishedEventArgs> BeforeSuiteFinished;

        public static EventHandler<TestItemFinishedEventArgs> AfterSuiteFinished;

        private void StartSuite(string report) => InvokeSafely(() => StartSuite(XElement.Parse(report)));

        private void StartSuite(XElement xElement)
        {
            var id = xElement.Attribute("id").Value;
            var name = xElement.Attribute("name").Value;
            var parentId = xElement.Attribute("parentId").Value;

            var startTime = DateTime.UtcNow;

            var startSuiteRequest = new StartTestItemRequest
            {
                StartTime = startTime,
                Name = name,
                Type = TestItemType.Suite
            };

            var report = xElement.ToString();
            var beforeSuiteEventArgs = new TestItemStartedEventArgs(_rpService, startSuiteRequest, null, report)
            {
                Canceled = Config.GetRootNamespaces()?.Any(root => root == name) ?? false
            };

            if (!beforeSuiteEventArgs.Canceled)
            {
                RiseEvent(BeforeSuiteStarted, beforeSuiteEventArgs, nameof(BeforeSuiteStarted));
                var suiteReporter = GetSuiteReporter(parentId, startSuiteRequest);

                _flowItems[id] = new FlowItemInfo(
                    id, parentId, FlowItemInfo.FlowType.Suite, name, suiteReporter, startTime);

                beforeSuiteEventArgs.TestReporter = suiteReporter;
                RiseEvent(AfterSuiteStarted, beforeSuiteEventArgs, nameof(AfterSuiteStarted));
            }
            else
            {
                _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Suite, name, null, startTime);
            }
        }

        private void FinishSuite(string report) => InvokeSafely(() => FinishSuite(XElement.Parse(report)));

        private void FinishSuite(XElement xElement)
        {
            var type = xElement.Attribute("type").Value;
            var id = xElement.Attribute("id").Value;
            var result = xElement.Attribute("result").Value;

            var parentId = xElement.Attribute("parentId");
            var duration = float.Parse(xElement.Attribute("duration").Value, CultureInfo.InvariantCulture);

            // at the end of execution nunit raises 2 the same events, we need only that which has 'parentId' xml tag
            if (parentId is null)
            {
                return;
            }

            var report = xElement.ToString();

            if (!_flowItems.ContainsKey(id))
            {
                StartSuite(report);
            }

            if (!_flowItems.ContainsKey(id))
            {
                return;
            }

            var finishSuiteRequest = new FinishTestItemRequest
            {
                EndTime = _flowItems[id].StartTime.AddSeconds(duration),
                Status = _statusMap[result]
            };

            HandleProperties(xElement, finishSuiteRequest);

            var eventArgs = new TestItemFinishedEventArgs(
                _rpService, finishSuiteRequest, _flowItems[id].TestReporter, report);

            RiseEvent(BeforeSuiteFinished, eventArgs, nameof(BeforeSuiteFinished));

            /* 
                Understand whether finishing test item should be deferred.
                Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
            */
            if (IsShouldBeDeferred(xElement))
            {
                _flowItems[id].FinishTestItemRequest = finishSuiteRequest;
                _flowItems[id].Report = report;
                _flowItems[id].DeferredFinishAction = GetFinishSuiteAction();
            }
            else
            {
                var failurestacktrace = xElement.XPathSelectElement("//failure/stack-trace")?.Value;
                GetFinishSuiteAction().Invoke(id, finishSuiteRequest, report, failurestacktrace);
            }
        }

        private Action<string, FinishTestItemRequest, string, string> GetFinishSuiteAction()
        {
            return (id, request, report, parentStackTrace) =>
            {
                // find all defferred children test items to finish
                var deferredFlowItems = _flowItems
                    .Where(fi => fi.Value.ParentId == id && fi.Value.DeferredFinishAction != null)
                    .Select(fi => fi.Value)
                    .ToList();

                foreach (var deferredFlowItem in deferredFlowItems)
                {
                    deferredFlowItem.DeferredFinishAction.Invoke(
                        deferredFlowItem.Id,
                        deferredFlowItem.FinishTestItemRequest,
                        deferredFlowItem.Report,
                        parentStackTrace);
                }

                var reporter = _flowItems[id].TestReporter;
                reporter?.Finish(request);

                var testFinishedEventArgs = new TestItemFinishedEventArgs(_rpService, request, reporter, report);
                RiseEvent(AfterSuiteFinished, testFinishedEventArgs, nameof(AfterSuiteFinished));

                _flowItems.Remove(id);
            };
        }

        private ITestReporter GetSuiteReporter(string parentId, StartTestItemRequest request)
        {
            if (!parentId.HasValue() || !_flowItems.ContainsKey(parentId))
            {
                return _launchReporter.StartChildTestReporter(request);
            }

            var parentFlowItem = FindReportedParentFlowItem(parentId);

            return parentFlowItem is null
                ? _launchReporter.StartChildTestReporter(request)
                : parentFlowItem.TestReporter.StartChildTestReporter(request);
        }

        private FlowItemInfo FindReportedParentFlowItem(string id)
        {
            return _flowItems[id].TestReporter is null
                ? (_flowItems[id].ParentId.HasValue() ? FindReportedParentFlowItem(_flowItems[id].ParentId) : null)
                : _flowItems[id];
        }
    }
}