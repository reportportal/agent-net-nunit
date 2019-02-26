﻿using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public delegate void SuiteStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event SuiteStartedHandler BeforeSuiteStarted;
        public static event SuiteStartedHandler AfterSuiteStarted;

        private void StartSuite(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId").Value;
                var name = xmlDoc.SelectSingleNode("/*/@name").Value;

                var startTime = DateTime.UtcNow;

                var startSuiteRequest = new StartTestItemRequest
                {
                    StartTime = startTime,
                    Name = name,
                    Type = TestItemType.Suite
                };

                var beforeSuiteEventArg = new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest, null, xmlDoc.OuterXml);
                try
                {
                    BeforeSuiteStarted?.Invoke(this, beforeSuiteEventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeSuiteStarted' subscriber." + Environment.NewLine + exp);
                }
                if (!beforeSuiteEventArg.Canceled)
                {
                    ITestReporter suiteReporter;
                    if (string.IsNullOrEmpty(parentId) || !_flowItems.ContainsKey(parentId))
                    {
                        suiteReporter = Bridge.Context.LaunchReporter.StartChildTestReporter(startSuiteRequest);
                    }
                    else
                    {
                        suiteReporter = _flowItems[parentId].TestReporter.StartChildTestReporter(startSuiteRequest);
                    }

                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Suite, name, suiteReporter, startTime);

                    try
                    {
                        AfterSuiteStarted?.Invoke(this, new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest, suiteReporter, xmlDoc.OuterXml));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterSuiteStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void SuiteFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event SuiteFinishedHandler BeforeSuiteFinished;
        public static event SuiteFinishedHandler AfterSuiteFinished;

        private void FinishSuite(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var result = xmlDoc.SelectSingleNode("/*/@result").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId");
                var duration = float.Parse(xmlDoc.SelectSingleNode("/*/@duration").Value, System.Globalization.CultureInfo.InvariantCulture);

                // at the end of execution nunit raises 2 the same events, we need only that which has 'parentId' xml tag
                if (parentId != null)
                {
                    if (!_flowItems.ContainsKey(id))
                    {
                        StartSuite(xmlDoc);
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
                        var categories = xmlDoc.SelectNodes("//properties/property[@name='Category']");
                        if (categories != null)
                        {
                            finishSuiteRequest.Tags = new List<string>();

                            foreach (XmlNode category in categories)
                            {
                                finishSuiteRequest.Tags.Add(category.Attributes["value"].Value);
                            }
                        }

                        // adding description to suite
                        var description = xmlDoc.SelectSingleNode("//properties/property[@name='Description']");
                        if (description != null)
                        {
                            finishSuiteRequest.Description = description.Attributes["value"].Value;
                        }

                        var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishSuiteRequest, _flowItems[id].TestReporter, xmlDoc.OuterXml);

                        try
                        {
                            BeforeSuiteFinished?.Invoke(this, eventArg);
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'BeforeSuiteFinished' subscriber." + Environment.NewLine + exp);
                        }

                        Action<string, FinishTestItemRequest, string, string> finishSuiteAction = (__id, __finishSuiteRequest, __report, __parentstacktrace) =>
                        {
                            // find all defferred children test items to finish
                            var deferredFlowItems = _flowItems.Where(fi => fi.Value.ParentId == __id && fi.Value.DeferredFinishAction != null).Select(fi => fi.Value).ToList();
                            foreach (var deferredFlowItem in deferredFlowItems)
                            {
                                deferredFlowItem.DeferredFinishAction.Invoke(deferredFlowItem.Id, deferredFlowItem.FinishTestItemRequest, deferredFlowItem.Report, __parentstacktrace);
                            }

                            _flowItems[__id].TestReporter.Finish(__finishSuiteRequest);

                            try
                            {
                                AfterSuiteFinished?.Invoke(this, new TestItemFinishedEventArgs(Bridge.Service, __finishSuiteRequest, _flowItems[__id].TestReporter, __report));
                            }
                            catch (Exception exp)
                            {
                                Console.WriteLine("Exception was thrown in 'AfterSuiteFinished' subscriber." + Environment.NewLine + exp);
                            }

                            _flowItems.Remove(__id);
                        };

                        // understand whether finishing test suite should be defferred. Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
                        if (xmlDoc.SelectSingleNode("/*/@site")?.Value == "Parent")
                        {
                            _flowItems[id].FinishTestItemRequest = finishSuiteRequest;
                            _flowItems[id].Report = xmlDoc.OuterXml;
                            _flowItems[id].DeferredFinishAction = finishSuiteAction;
                        }
                        else
                        {
                            var failurestacktrace = xmlDoc.SelectSingleNode("//failure/stack-trace")?.InnerText;

                            finishSuiteAction.Invoke(id, finishSuiteRequest, xmlDoc.OuterXml, failurestacktrace);
                        }
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