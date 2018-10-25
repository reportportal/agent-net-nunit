using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

                var beforeSuiteEventArg = new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest);
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
                    TestReporter suiteReporter;
                    if (string.IsNullOrEmpty(parentId) || !_flowItems.ContainsKey(parentId))
                    {
                        suiteReporter = Bridge.Context.LaunchReporter.StartNewTestNode(startSuiteRequest);
                    }
                    else
                    {
                        suiteReporter = _flowItems[parentId].Reporter.StartNewTestNode(startSuiteRequest);
                    }

                    _flowItems[id] = new FlowItemInfo(FlowItemInfo.FlowType.Suite, name, suiteReporter, startTime);

                    try
                    {
                        AfterSuiteStarted?.Invoke(this, new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest, suiteReporter));
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
                            EndTime = _flowItems[id].StartTime.AddMilliseconds(duration),
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

                        var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishSuiteRequest, _flowItems[id].Reporter, xmlDoc.InnerXml);

                        try
                        {
                            BeforeSuiteFinished?.Invoke(this, eventArg);
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'BeforeSuiteFinished' subscriber." + Environment.NewLine + exp);
                        }

                        _flowItems[id].Reporter.Finish(finishSuiteRequest);

                        try
                        {
                            AfterSuiteFinished?.Invoke(this, new TestItemFinishedEventArgs(Bridge.Service, finishSuiteRequest, _flowItems[id].Reporter, xmlDoc.InnerXml));
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'AfterSuiteFinished' subscriber." + Environment.NewLine + exp);
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