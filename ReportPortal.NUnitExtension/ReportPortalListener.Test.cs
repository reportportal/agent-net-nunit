using ReportPortal.Client.Converters;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

                var beforeTestEventArg = new TestItemStartedEventArgs(Bridge.Service, startTestRequest);
                try
                {
                    if (BeforeTestStarted != null) BeforeTestStarted(this, beforeTestEventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeTestStarted' subscriber." + Environment.NewLine +
                                      exp);
                }
                if (!beforeTestEventArg.Canceled)
                {
                    var testReporter = _flowItems[parentId].Reporter.StartNewTestNode(startTestRequest);

                    _flowItems[id] = new FlowItemInfo(FlowItemInfo.FlowType.Test, fullname, testReporter, startTime);

                    try
                    {
                        if (AfterTestStarted != null)
                            AfterTestStarted(this, new TestItemStartedEventArgs(Bridge.Service, startTestRequest, testReporter));
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

                        var outputEventArgs = new TestItemOutputEventArgs(Bridge.Service, outputLogRequest, _flowItems[id].Reporter);

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
                            _flowItems[id].Reporter.Log(outputLogRequest);

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

                        _flowItems[id].Reporter.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Info,
                            Time = DateTime.UtcNow,
                            Text = fileDescription != null ? fileDescription : System.IO.Path.GetFileName(filePath),
                            Attach = new Client.Models.Attach
                            {
                                Name = System.IO.Path.GetFileName(filePath),
                                MimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(System.IO.Path.GetExtension(filePath)),
                                Data = System.IO.File.ReadAllBytes(filePath)
                            }
                        });
                    }

                    // adding failure message
                    var failureNode = xmlDoc.SelectSingleNode("//failure");
                    if (failureNode != null)
                    {
                        var failureMessage = failureNode.SelectSingleNode("./message").InnerText;
                        var failureStacktrace = failureNode.SelectSingleNode("./stack-trace")?.InnerText;

                        _flowItems[id].Reporter.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = failureMessage + Environment.NewLine + failureStacktrace
                        });
                    }

                    // adding reason message
                    var reasonNode = xmlDoc.SelectSingleNode("//reason");
                    if (reasonNode != null)
                    {
                        var reasonMessage = reasonNode.SelectSingleNode("./message").InnerText;

                        _flowItems[id].Reporter.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = reasonMessage
                        });
                    }

                    // finishing test
                    var finishTestRequest = new FinishTestItemRequest
                    {
                        EndTime = _flowItems[id].StartTime.AddMilliseconds(duration),
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

                    var properties = new Dictionary<string, string>();
                    var propertiesNodes = xmlDoc.SelectNodes("//properties/property");
                    if (propertiesNodes != null)
                    {
                        foreach (XmlNode propertyNode in propertiesNodes)
                        {
                            properties.Add(propertyNode.Attributes["name"].Value, propertyNode.Attributes["value"].Value);
                        }
                    }

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishTestRequest, _flowItems[id].Reporter, properties);

                    try
                    {
                        BeforeTestFinished?.Invoke(this, eventArg);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'BeforeTestFinished' subscriber." + Environment.NewLine + exp);
                    }

                    _flowItems[id].Reporter.Finish(finishTestRequest);

                    try
                    {
                        AfterTestFinished?.Invoke(this, new TestItemFinishedEventArgs(Bridge.Service, finishTestRequest, _flowItems[id].Reporter, properties));
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
                        _flowItems[id].Reporter.Log(logRequest);
                    }
                    else
                    {
                        _flowItems[id].Reporter.Log(new AddLogItemRequest { Level = LogLevel.Info, Time = DateTime.UtcNow, Text = message });
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
                        _flowItems[testId].Reporter.Log(logRequest);
                    }
                    else
                    {
                        _flowItems[testId].Reporter.Log(new AddLogItemRequest { Level = LogLevel.Info, Time = DateTime.UtcNow, Text = message });
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
