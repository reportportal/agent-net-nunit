using ReportPortal.Client.Converters;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                var startTime = DateTime.UtcNow;

                var startTestRequest = new StartTestItemRequest
                {
                    StartTime = startTime,
                    Name = name,
                    Type = TestItemType.Step
                };

                var beforeTestEventArg = new TestItemStartedEventArgs(Bridge.Service, startTestRequest, null, xmlDoc.OuterXml);
                try
                {
                    BeforeTestStarted?.Invoke(this, beforeTestEventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeTestStarted' subscriber." + Environment.NewLine +
                                      exp);
                }
                if (!beforeTestEventArg.Canceled)
                {
                    var testReporter = _flowItems[parentId].TestReporter.StartChildTestReporter(startTestRequest);

                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Test, fullname, testReporter, startTime);

                    try
                    {
                        AfterTestStarted?.Invoke(this, new TestItemStartedEventArgs(Bridge.Service, startTestRequest, testReporter, xmlDoc.OuterXml));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterTestStarted' subscriber." + Environment.NewLine +
                                          exp);
                    }
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

        public delegate void TestOutputHandler(object sender, TestItemOutputEventArgs e);

        public static event TestOutputHandler BeforeTestOutput;
        public static event TestOutputHandler AfterTestOutput;

        public void FinishTest(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var result = xmlDoc.SelectSingleNode("/*/@result").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId");
                var duration = float.Parse(xmlDoc.SelectSingleNode("/*/@duration").Value, System.Globalization.CultureInfo.InvariantCulture);

                if (!_flowItems.ContainsKey(id))
                {
                    StartTest(xmlDoc);
                }

                if (_flowItems.ContainsKey(id))
                {
                    // adding console output
                    var outputNode = xmlDoc.SelectSingleNode("//output");
                    if (outputNode != null)
                    {
                        var outputLogRequest = new AddLogItemRequest
                        {
                            Level = LogLevel.Trace,
                            Time = DateTime.UtcNow,
                            Text = "Test Output: " + Environment.NewLine + outputNode.InnerText
                        };

                        var outputEventArgs = new TestItemOutputEventArgs(Bridge.Service, outputLogRequest, _flowItems[id].TestReporter, xmlDoc.OuterXml);

                        try
                        {
                            BeforeTestOutput?.Invoke(this, outputEventArgs);
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'BeforeTestOutput' subscriber." + Environment.NewLine + exp);
                        }

                        if (!outputEventArgs.Canceled)
                        {
                            _flowItems[id].TestReporter.Log(outputLogRequest);

                            try
                            {
                                AfterTestOutput?.Invoke(this, outputEventArgs);
                            }
                            catch (Exception exp)
                            {
                                Console.WriteLine("Exception was thrown in 'AfterTestOutput' subscriber." + Environment.NewLine + exp);
                            }
                        }
                    }

                    // adding attachments
                    var attachmentNodes = xmlDoc.SelectNodes("//attachments/attachment");
                    foreach (XmlNode attachmentNode in attachmentNodes)
                    {
                        var filePath = attachmentNode.SelectSingleNode("./filePath").InnerText;
                        var fileDescription = attachmentNode.SelectSingleNode("./description")?.InnerText;

                        if (File.Exists(filePath))
                        {
                            _flowItems[id].TestReporter.Log(new AddLogItemRequest
                            {
                                Level = LogLevel.Info,
                                Time = DateTime.UtcNow,
                                Text = fileDescription != null ? fileDescription : Path.GetFileName(filePath),
                                Attach = new Client.Models.Attach
                                {
                                    Name = Path.GetFileName(filePath),
                                    MimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(filePath)),
                                    Data = File.ReadAllBytes(filePath)
                                }
                            });
                        }
                        else
                        {
                            _flowItems[id].TestReporter.Log(new AddLogItemRequest
                            {
                                Level = LogLevel.Warning,
                                Time = DateTime.UtcNow,
                                Text = $"Attachment file '{filePath}' doesn't exists.",
                            });
                        }
                    }

                    // adding failure message
                    var failureNode = xmlDoc.SelectSingleNode("//failure");
                    if (failureNode != null)
                    {
                        var failureMessage = failureNode.SelectSingleNode("./message")?.InnerText;
                        var failureStacktrace = failureNode.SelectSingleNode("./stack-trace")?.InnerText;

                        _flowItems[id].TestReporter.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = string.Join(Environment.NewLine, new List<string> { failureMessage, failureStacktrace }.Where(m => !string.IsNullOrEmpty(m)))
                        });

                        // walk through assertions
                        foreach(XmlNode assertionNode in xmlDoc.SelectNodes("test-case/assertions/assertion"))
                        {
                            var assertionMessage = assertionNode.SelectSingleNode("message")?.InnerText;
                            var assertionStacktrace = assertionNode.SelectSingleNode("stack-trace")?.InnerText;

                            if (assertionMessage != failureMessage && assertionStacktrace != failureStacktrace)
                            {
                                _flowItems[id].TestReporter.Log(new AddLogItemRequest
                                {
                                    Level = LogLevel.Error,
                                    Time = DateTime.UtcNow,
                                    Text = string.Join(Environment.NewLine, new List<string> { assertionMessage, assertionStacktrace }.Where(m => !string.IsNullOrEmpty(m)))
                                });
                            }
                        }
                    }

                    // adding reason message
                    var reasonNode = xmlDoc.SelectSingleNode("//reason");
                    if (reasonNode != null)
                    {
                        var reasonMessage = reasonNode.SelectSingleNode("./message")?.InnerText;

                        _flowItems[id].TestReporter.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = $"Reason: {reasonMessage}"
                        });
                    }

                    // finishing test
                    var finishTestRequest = new FinishTestItemRequest
                    {
                        EndTime = _flowItems[id].StartTime.AddSeconds(duration),
                        Status = _statusMap[result]
                    };

                    // adding categories to test
                    var categories = xmlDoc.SelectNodes("//properties/property[@name='Category']");
                    if (categories != null)
                    {
                        finishTestRequest.Tags = new List<string>();

                        foreach (XmlNode category in categories)
                        {
                            finishTestRequest.Tags.Add(category.Attributes["value"].Value);
                        }
                    }

                    // adding description to test
                    var description = xmlDoc.SelectSingleNode("//properties/property[@name='Description']");
                    if (description != null)
                    {
                        finishTestRequest.Description = description.Attributes["value"].Value;
                    }

                    var isRetry = xmlDoc.SelectSingleNode("//properties/property[@name='Retry']");
                    if (isRetry != null)
                    {
                        finishTestRequest.IsRetry = true;
                    }

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishTestRequest, _flowItems[id].TestReporter, xmlDoc.OuterXml);

                    try
                    {
                        BeforeTestFinished?.Invoke(this, eventArg);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'BeforeTestFinished' subscriber." + Environment.NewLine + exp);
                    }

                    Action<string, FinishTestItemRequest, string, string> finishTestAction = (__id, __finishTestItemRequest, __report, __parentstacktrace) =>
                    {
                        if (!string.IsNullOrEmpty(__parentstacktrace))
                        {
                            _flowItems[__id].TestReporter.Log(new AddLogItemRequest
                            {
                                Level = LogLevel.Error,
                                Time = DateTime.UtcNow,
                                Text = __parentstacktrace
                            });
                        }

                        _flowItems[__id].TestReporter.Finish(__finishTestItemRequest);

                        try
                        {
                            AfterTestFinished?.Invoke(this, new TestItemFinishedEventArgs(Bridge.Service, __finishTestItemRequest, _flowItems[__id].TestReporter, __report));
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'AfterTestFinished' subscriber." + Environment.NewLine + exp);
                        }

                        _flowItems.Remove(__id);
                    };
                    // understand whether finishing test item should be deferred. Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
                    if (xmlDoc.SelectSingleNode("/*/@site")?.Value == "Parent")
                    {
                        _flowItems[id].FinishTestItemRequest = finishTestRequest;
                        _flowItems[id].Report = xmlDoc.OuterXml;
                        _flowItems[id].DeferredFinishAction = finishTestAction;
                    }
                    else
                    {
                        finishTestAction.Invoke(id, finishTestRequest, xmlDoc.OuterXml, null);
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
                var id = xmlDoc.SelectSingleNode("/test-output/@testid").Value;
                var message = xmlDoc.SelectSingleNode("/test-output").InnerText;

                if (_flowItems.ContainsKey(id))
                {
                    AddLogItemRequest logRequest = null;
                    try
                    {
                        var sharedMessage = ModelSerializer.Deserialize<SharedLogMessage>(message);

                        logRequest = new AddLogItemRequest
                        {
                            Level = sharedMessage.Level,
                            Time = sharedMessage.Time,
                            TestItemId = sharedMessage.TestItemId,
                            Text = sharedMessage.Text
                        };
                        if (sharedMessage.Attach != null)
                        {
                            logRequest.Attach = new Client.Models.Attach
                            {
                                Name = sharedMessage.Attach.Name,
                                MimeType = sharedMessage.Attach.MimeType,
                                Data = sharedMessage.Attach.Data
                            };
                        }
                    }
                    catch (Exception)
                    {

                    }

                    if (logRequest != null)
                    {
                        _flowItems[id].TestReporter.Log(logRequest);
                    }
                    else
                    {
                        _flowItems[id].TestReporter.Log(new AddLogItemRequest { Level = LogLevel.Info, Time = DateTime.UtcNow, Text = message });
                    }

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public void TestMessage(XmlDocument xmlDoc)
        {
            try
            {
                var testId = xmlDoc.SelectSingleNode("/test-message/@testid").Value;
                var message = xmlDoc.SelectSingleNode("/test-message").InnerText;

                if (_flowItems.ContainsKey(testId))
                {
                    AddLogItemRequest logRequest = null;
                    try
                    {
                        var sharedMessage = ModelSerializer.Deserialize<SharedLogMessage>(message);

                        logRequest = new AddLogItemRequest
                        {
                            Level = sharedMessage.Level,
                            Time = sharedMessage.Time,
                            TestItemId = sharedMessage.TestItemId,
                            Text = sharedMessage.Text
                        };
                        if (sharedMessage.Attach != null)
                        {
                            logRequest.Attach = new Client.Models.Attach
                            {
                                Name = sharedMessage.Attach.Name,
                                MimeType = sharedMessage.Attach.MimeType,
                                Data = sharedMessage.Attach.Data
                            };
                        }
                    }
                    catch (Exception)
                    {

                    }

                    if (logRequest != null)
                    {
                        _flowItems[testId].TestReporter.Log(logRequest);
                    }
                    else
                    {
                        _flowItems[testId].TestReporter.Log(new AddLogItemRequest { Level = LogLevel.Info, Time = DateTime.UtcNow, Text = message });
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
