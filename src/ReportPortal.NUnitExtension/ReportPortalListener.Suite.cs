using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared.Execution.Metadata;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public delegate void SuiteStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event SuiteStartedHandler BeforeSuiteStarted;
        public static event SuiteStartedHandler AfterSuiteStarted;

        private void StartSuite(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var type = xElement.Attribute("type").Value;

                var id = xElement.Attribute("id").Value;
                var parentId = xElement.Attribute("parentId").Value;
                var name = xElement.Attribute("name").Value;
                var fullname = xElement.Attribute("fullname").Value;

                var startTime = DateTime.UtcNow;

                var startSuiteRequest = new StartTestItemRequest
                {
                    StartTime = startTime,
                    Name = name,
                    Type = TestItemType.Suite
                };

                var beforeSuiteEventArg = new TestItemStartedEventArgs(_rpService, startSuiteRequest, null, report);

                var rootNamespaces = Config.GetValues<string>("rootNamespaces", null);
                if (rootNamespaces != null && rootNamespaces.Any(n => n == name))
                {
                    beforeSuiteEventArg.Canceled = true;
                }

                if (!beforeSuiteEventArg.Canceled)
                {
                    try
                    {
                        BeforeSuiteStarted?.Invoke(this, beforeSuiteEventArg);
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'BeforeSuiteStarted' subscriber." + Environment.NewLine + exp);
                    }
                }

                if (!beforeSuiteEventArg.Canceled)
                {
                    ITestReporter suiteReporter;

                    if (string.IsNullOrEmpty(parentId) || !_flowItems.ContainsKey(parentId))
                    {
                        suiteReporter = _launchReporter.StartChildTestReporter(startSuiteRequest);
                    }
                    else
                    {
                        var parentFlowItem = FindReportedParentFlowItem(parentId);
                        if (parentFlowItem == null)
                        {
                            suiteReporter = _launchReporter.StartChildTestReporter(startSuiteRequest);
                        }
                        else
                        {
                            suiteReporter = parentFlowItem.TestReporter.StartChildTestReporter(startSuiteRequest);
                        }
                    }

                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Suite, name, suiteReporter, startTime);

                    try
                    {
                        AfterSuiteStarted?.Invoke(this, new TestItemStartedEventArgs(_rpService, startSuiteRequest, suiteReporter, report));
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'AfterSuiteStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
                else
                {
                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Suite, name, null, startTime);
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        private FlowItemInfo FindReportedParentFlowItem(string id)
        {
            if (_flowItems[id].TestReporter != null)
            {
                return _flowItems[id];
            }
            else if (!string.IsNullOrEmpty(_flowItems[id].ParentId))
            {
                return FindReportedParentFlowItem(_flowItems[id].ParentId);
            }
            else return null;
        }

        public delegate void SuiteFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event SuiteFinishedHandler BeforeSuiteFinished;
        public static event SuiteFinishedHandler AfterSuiteFinished;

        private void FinishSuite(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var type = xElement.Attribute("type").Value;

                var id = xElement.Attribute("id").Value;
                var result = xElement.Attribute("result").Value;
                var parentId = xElement.Attribute("parentId");
                var duration = float.Parse(xElement.Attribute("duration").Value, System.Globalization.CultureInfo.InvariantCulture);

                // at the end of execution nunit raises 2 the same events, we need only that which has 'parentId' xml tag
                if (parentId != null)
                {
                    if (!_flowItems.ContainsKey(id))
                    {
                        StartSuite(report);
                    }

                    if (_flowItems.ContainsKey(id))
                    {
                        // finishing suite
                        var finishSuiteRequest = new FinishTestItemRequest
                        {
                            EndTime = _flowItems[id].StartTime.AddSeconds(duration),
                            Status = _statusMap[result]
                        };

                        // adding categories to suite
                        var categories = xElement.XPathSelectElements("//properties/property[@name='Category']");
                        if (categories != null)
                        {
                            finishSuiteRequest.Attributes = new List<ItemAttribute>();

                            foreach (XElement category in categories)
                            {
                                var metaAttribute = MetaAttribute.Parse(category.Attribute("value").Value);
                                var attr = (ItemAttribute)metaAttribute;
                                if (string.IsNullOrEmpty(attr.Key))
                                {
                                    attr.Key = "Category";
                                }
                                finishSuiteRequest.Attributes.Add(attr);
                            }
                        }

                        // adding description to suite
                        var description = xElement.XPathSelectElement("//properties/property[@name='Description']");
                        if (description != null)
                        {
                            finishSuiteRequest.Description = description.Attribute("value").Value;
                        }

                        var eventArg = new TestItemFinishedEventArgs(_rpService, finishSuiteRequest, _flowItems[id].TestReporter, report);

                        try
                        {
                            BeforeSuiteFinished?.Invoke(this, eventArg);
                        }
                        catch (Exception exp)
                        {
                            _traceLogger.Error("Exception was thrown in 'BeforeSuiteFinished' subscriber." + Environment.NewLine + exp);
                        }

                        Action<string, FinishTestItemRequest, string, string> finishSuiteAction = (__id, __finishSuiteRequest, __report, __parentstacktrace) =>
                        {
                            // find all defferred children test items to finish
                            var deferredFlowItems = _flowItems.Where(fi => fi.Value.ParentId == __id && fi.Value.DeferredFinishAction != null).Select(fi => fi.Value).ToList();
                            foreach (var deferredFlowItem in deferredFlowItems)
                            {
                                deferredFlowItem.DeferredFinishAction.Invoke(deferredFlowItem.Id, deferredFlowItem.FinishTestItemRequest, deferredFlowItem.Report, __parentstacktrace);
                            }

                            _flowItems[__id].TestReporter?.Finish(__finishSuiteRequest);

                            try
                            {
                                AfterSuiteFinished?.Invoke(this, new TestItemFinishedEventArgs(_rpService, __finishSuiteRequest, _flowItems[__id].TestReporter, __report));
                            }
                            catch (Exception exp)
                            {
                                _traceLogger.Error("Exception was thrown in 'AfterSuiteFinished' subscriber." + Environment.NewLine + exp);
                            }

                            _flowItems.Remove(__id);
                        };

                        // understand whether finishing test suite should be defferred. Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
                        if (xElement.Attribute("site")?.Value == "Parent")
                        {
                            _flowItems[id].FinishTestItemRequest = finishSuiteRequest;
                            _flowItems[id].Report = report;
                            _flowItems[id].DeferredFinishAction = finishSuiteAction;
                        }
                        else
                        {
                            var failurestacktrace = xElement.XPathSelectElement("//failure/stack-trace")?.Value;

                            finishSuiteAction.Invoke(id, finishSuiteRequest, report, failurestacktrace);
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }
    }
}