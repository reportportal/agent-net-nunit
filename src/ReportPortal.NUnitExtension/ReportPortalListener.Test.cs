using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public delegate void TestStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event TestStartedHandler BeforeTestStarted;
        public static event TestStartedHandler AfterTestStarted;

        public void StartTest(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId").Value;
                var name = xmlDoc.SelectSingleNode("/*/@name").Value;
                var fullname = xmlDoc.SelectSingleNode("/*/@fullname").Value;

                var startTestRequest = new StartTestItemRequest
                {
                    LaunchId = Bridge.Context.LaunchId,
                    StartTime = DateTime.UtcNow,
                    Name = name,
                    Type = TestItemType.Step,
                    Tags = Config.Launch.Tags
                };

                var beforeTestEventArg = new TestItemStartedEventArgs(Bridge.Service, startTestRequest);
                try
                {
                    if (BeforeTestStarted != null) BeforeTestStarted(this, beforeTestEventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeTestStarted' subscriber." + Environment.NewLine + exp);
                }
                if (!beforeTestEventArg.Canceled)
                {
                    TestItem test;

                    test = Bridge.Service.StartTestItem(_suitesFlow[parentId].Id, startTestRequest);

                    _testFlowIds[id] = beforeTestEventArg;
                    beforeTestEventArg.Id = test.Id;
                    Bridge.Context.TestId = test.Id;

                    _testFlowNames[fullname] = test.Id;

                    try
                    {
                        if (AfterTestStarted != null) AfterTestStarted(this, new TestItemStartedEventArgs(Bridge.Service, startTestRequest, test.Id));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterTestStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
                else
                {
                    _testFlowIds[id] = beforeTestEventArg;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void TestFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event TestFinishedHandler BeforeTestFinished;
        public static event TestFinishedHandler AfterTestFinished;

        public void FinishTest(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var result = xmlDoc.SelectSingleNode("/*/@result").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId");


                if (!_testFlowIds[id].Canceled)
                {
                    var updateTestRequest = new UpdateTestItemRequest();

                    // adding tags to test
                    updateTestRequest.Tags = Config.Launch.Tags;

                    // adding categories to test
                    var categories = xmlDoc.SelectNodes("//properties/property[@name='Category']");
                    if (categories != null)
                    {
                        foreach (XmlNode category in categories)
                        {
                            updateTestRequest.Tags.Add(category.Attributes["value"].Value);
                        }
                    }

                    // adding description to test
                    var description = xmlDoc.SelectSingleNode("//properties/property[@name='Description']");
                    if (description != null)
                    {
                        updateTestRequest.Description = description.Attributes["value"].Value;
                    }

                    if (updateTestRequest.Description != null || updateTestRequest.Tags != null)
                    {
                        Bridge.Service.UpdateTestItem(_testFlowIds[id].Id, updateTestRequest);
                    }

                    // adding console output
                    var outputNode = xmlDoc.SelectSingleNode("//output");
                    if (outputNode != null)
                    {
                        Bridge.Service.AddLogItem(new AddLogItemRequest
                        {
                            Level = LogLevel.Trace,
                            TestItemId = _testFlowIds[id].Id,
                            Time = DateTime.UtcNow,
                            Text = "Test Output: " + Environment.NewLine + outputNode.InnerText
                        });
                    }

                    // adding failure message
                    var failureNode = xmlDoc.SelectSingleNode("//failure");
                    if (failureNode != null)
                    {
                        var failureMessage = failureNode.SelectSingleNode("./message").InnerText;
                        var failureStacktrace = failureNode.SelectSingleNode("./stack-trace").InnerText;

                        Bridge.Service.AddLogItem(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            TestItemId = _testFlowIds[id].Id,
                            Time = DateTime.UtcNow,
                            Text = failureMessage + Environment.NewLine + failureStacktrace
                        });
                    }

                    // finishing test
                    var finishTestRequest = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = _statusMap[result]
                    };

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishTestRequest, result, _testFlowIds[id].Id);

                    try
                    {
                        if (BeforeTestFinished != null) BeforeTestFinished(this, eventArg);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'BeforeTestFinished' subscriber." + Environment.NewLine + exp);
                    }

                    var message = Bridge.Service.FinishTestItem(_testFlowIds[id].Id, finishTestRequest).Info;
                    Bridge.Context.TestId = null;

                    try
                    {
                        if (AfterTestFinished != null) AfterTestFinished(this, new TestItemFinishedEventArgs(Bridge.Service, finishTestRequest, message, _testFlowIds[id].Id));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterTestFinished' subscriber." + Environment.NewLine + exp);
                    }
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public void TestOutput(XmlDocument xmlDoc)
        {
            try
            {
                var fullTestName = xmlDoc.SelectSingleNode("/test-output/@testname").Value;
                var message = xmlDoc.SelectSingleNode("/test-output").InnerText;

                if (_testFlowNames.ContainsKey(fullTestName))
                {
                    Bridge.Service.AddLogItem(new AddLogItemRequest
                    {
                        Level = LogLevel.Info,
                        TestItemId = _testFlowNames[fullTestName],
                        Time = DateTime.UtcNow,
                        Text = message
                    });
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }
    }
}
