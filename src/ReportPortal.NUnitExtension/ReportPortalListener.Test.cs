using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Converters;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public delegate void TestStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event TestStartedHandler BeforeTestStarted;
        public static event TestStartedHandler AfterTestStarted;

        public void StartTest(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var id = xElement.Attribute("id").Value;
                var parentId = xElement.Attribute("parentId").Value;
                var name = xElement.Attribute("name").Value;
                var fullname = xElement.Attribute("fullname").Value;

                var startTime = DateTime.UtcNow;

                var startTestRequest = new StartTestItemRequest
                {
                    StartTime = startTime,
                    Name = name,
                    Type = TestItemType.Step,
                    TestCaseId = fullname,
                    CodeReference = ExtractCodeReferenceFromFullName(fullname)
                };

                var beforeTestEventArg = new TestItemStartedEventArgs(_rpService, startTestRequest, null, report);
                try
                {
                    BeforeTestStarted?.Invoke(this, beforeTestEventArg);
                }
                catch (Exception exp)
                {
                    _traceLogger.Error("Exception was thrown in 'BeforeTestStarted' subscriber." + Environment.NewLine + exp);
                }
                if (!beforeTestEventArg.Canceled)
                {
                    var testReporter = _flowItems[parentId].TestReporter.StartChildTestReporter(startTestRequest);

                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Test, fullname, testReporter, startTime);

                    try
                    {
                        AfterTestStarted?.Invoke(this, new TestItemStartedEventArgs(_rpService, startTestRequest, testReporter, report));
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'AfterTestStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void TestFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event TestFinishedHandler BeforeTestFinished;
        public static event TestFinishedHandler AfterTestFinished;

        public delegate void TestOutputHandler(object sender, TestItemOutputEventArgs e);

        public static event TestOutputHandler BeforeTestOutput;
        public static event TestOutputHandler AfterTestOutput;

        public void FinishTest(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var id = xElement.Attribute("id").Value;
                var result = xElement.Attribute("result").Value;
                var parentId = xElement.Attribute("parentId");
                var duration = float.Parse(xElement.Attribute("duration").Value, System.Globalization.CultureInfo.InvariantCulture);

                if (!_flowItems.ContainsKey(id))
                {
                    StartTest(report);
                }

                if (_flowItems.ContainsKey(id))
                {
                    // adding console output
                    var outputNode = xElement.XPathSelectElement("//output");
                    if (outputNode != null)
                    {
                        var outputLogRequest = new CreateLogItemRequest
                        {
                            Level = LogLevel.Trace,
                            Time = DateTime.UtcNow,
                            Text = "Test Output: " + Environment.NewLine + outputNode.Value
                        };

                        var outputEventArgs = new TestItemOutputEventArgs(_rpService, outputLogRequest, _flowItems[id].TestReporter, report);

                        try
                        {
                            BeforeTestOutput?.Invoke(this, outputEventArgs);
                        }
                        catch (Exception exp)
                        {
                            _traceLogger.Error("Exception was thrown in 'BeforeTestOutput' subscriber." + Environment.NewLine + exp);
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
                                _traceLogger.Error("Exception was thrown in 'AfterTestOutput' subscriber." + Environment.NewLine + exp);
                            }
                        }
                    }

                    // adding attachments
                    var attachmentNodes = xElement.XPathSelectElements("//attachments/attachment");
                    foreach (XElement attachmentNode in attachmentNodes)
                    {
                        var filePath = attachmentNode.XPathSelectElement("./filePath").Value;
                        var fileDescription = attachmentNode.XPathSelectElement("./description")?.Value;

                        if (File.Exists(filePath))
                        {
                            try
                            {
                                var attachmentLogItemRequest = new CreateLogItemRequest
                                {
                                    Level = LogLevel.Info,
                                    Time = DateTime.UtcNow,
                                    Text = fileDescription != null ? fileDescription : Path.GetFileName(filePath)
                                };

                                attachmentLogItemRequest.Attach = new LogItemAttach
                                {
                                    Name = Path.GetFileName(filePath),
                                    MimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(filePath)),
                                    Data = File.ReadAllBytes(filePath)
                                };

                                _flowItems[id].TestReporter.Log(attachmentLogItemRequest);
                            }
                            catch (Exception attachmentExp)
                            {
                                _flowItems[id].TestReporter.Log(new CreateLogItemRequest
                                {
                                    Level = LogLevel.Warning,
                                    Time = DateTime.UtcNow,
                                    Text = $"Cannot read '{filePath}' file: {attachmentExp}"
                                });
                            }
                        }
                        else
                        {
                            _flowItems[id].TestReporter.Log(new CreateLogItemRequest
                            {
                                Level = LogLevel.Warning,
                                Time = DateTime.UtcNow,
                                Text = $"Attachment file '{filePath}' doesn't exists."
                            });
                        }
                    }

                    // adding failure message
                    var failureNode = xElement.XPathSelectElement("//failure");
                    if (failureNode != null)
                    {
                        var failureMessage = failureNode.XPathSelectElement("./message")?.Value;
                        var failureStacktrace = failureNode.XPathSelectElement("./stack-trace")?.Value;

                        _flowItems[id].TestReporter.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = string.Join(Environment.NewLine, new List<string> { failureMessage, failureStacktrace }.Where(m => !string.IsNullOrEmpty(m)))
                        });

                        // walk through assertions
                        foreach (XElement assertionNode in xElement.XPathSelectElements("test-case/assertions/assertion"))
                        {
                            var assertionMessage = assertionNode.XPathSelectElement("message")?.Value;
                            var assertionStacktrace = assertionNode.XPathSelectElement("stack-trace")?.Value;

                            if (assertionMessage != failureMessage && assertionStacktrace != failureStacktrace)
                            {
                                _flowItems[id].TestReporter.Log(new CreateLogItemRequest
                                {
                                    Level = LogLevel.Error,
                                    Time = DateTime.UtcNow,
                                    Text = string.Join(Environment.NewLine, new List<string> { assertionMessage, assertionStacktrace }.Where(m => !string.IsNullOrEmpty(m)))
                                });
                            }
                        }
                    }

                    // adding reason message
                    var reasonNode = xElement.XPathSelectElement("//reason");
                    if (reasonNode != null)
                    {
                        var reasonMessage = reasonNode.XPathSelectElement("./message")?.Value;

                        _flowItems[id].TestReporter.Log(new CreateLogItemRequest
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
                    var categories = xElement.XPathSelectElements("//properties/property[@name='Category']");
                    if (categories != null)
                    {
                        if (finishTestRequest.Attributes == null)
                        {
                            finishTestRequest.Attributes = new List<ItemAttribute>();
                        }

                        foreach (XElement category in categories)
                        {
                            var value = category.Attribute("value").Value;

                            if (!string.IsNullOrEmpty(value))
                            {
                                var attr = new ItemAttributeConverter().ConvertFrom(value, opts => opts.UndefinedKey = "Category");

                                finishTestRequest.Attributes.Add(attr);
                            }
                        }
                    }

                    // adding author attribute to test
                    var authorElements = xElement.XPathSelectElements("//properties/property[@name='Author']");
                    if (authorElements != null)
                    {
                        if (finishTestRequest == null)
                        {
                            finishTestRequest.Attributes = new List<ItemAttribute>();
                        }

                        foreach (XElement authorElement in authorElements)
                        {
                            var value = authorElement.Attribute("value").Value;

                            if (!string.IsNullOrEmpty(value))
                            {
                                var attr = new ItemAttribute { Key = "Author", Value = value };

                                finishTestRequest.Attributes.Add(attr);
                            }
                        }
                    }

                    // adding description to test
                    var description = xElement.XPathSelectElement("//properties/property[@name='Description']");
                    if (description != null)
                    {
                        finishTestRequest.Description = description.Attribute("value").Value;
                    }

                    var isRetry = xElement.XPathSelectElement("//properties/property[@name='Retry']");
                    if (isRetry != null)
                    {
                        finishTestRequest.IsRetry = true;
                    }

                    var eventArg = new TestItemFinishedEventArgs(_rpService, finishTestRequest, _flowItems[id].TestReporter, report);

                    try
                    {
                        BeforeTestFinished?.Invoke(this, eventArg);
                    }
                    catch (Exception exp)
                    {
                        _traceLogger.Error("Exception was thrown in 'BeforeTestFinished' subscriber." + Environment.NewLine + exp);
                    }

                    Action<string, FinishTestItemRequest, string, string> finishTestAction = (__id, __finishTestItemRequest, __report, __parentstacktrace) =>
                    {
                        if (!string.IsNullOrEmpty(__parentstacktrace))
                        {
                            _flowItems[__id].TestReporter.Log(new CreateLogItemRequest
                            {
                                Level = LogLevel.Error,
                                Time = DateTime.UtcNow,
                                Text = __parentstacktrace
                            });
                        }

                        _flowItems[__id].TestReporter.Finish(__finishTestItemRequest);

                        try
                        {
                            AfterTestFinished?.Invoke(this, new TestItemFinishedEventArgs(_rpService, __finishTestItemRequest, _flowItems[__id].TestReporter, __report));
                        }
                        catch (Exception exp)
                        {
                            _traceLogger.Error("Exception was thrown in 'AfterTestFinished' subscriber." + Environment.NewLine + exp);
                        }

                        _flowItems.Remove(__id);
                    };
                    // understand whether finishing test item should be deferred. Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
                    if (xElement.Attribute("site")?.Value == "Parent")
                    {
                        _flowItems[id].FinishTestItemRequest = finishTestRequest;
                        _flowItems[id].Report = report;
                        _flowItems[id].DeferredFinishAction = finishTestAction;
                    }
                    else
                    {
                        finishTestAction.Invoke(id, finishTestRequest, report, null);
                    }
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public void TestOutput(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var id = xElement.Attribute("testid").Value;
                var message = xElement.Value;

                if (_flowItems.ContainsKey(id))
                {
                    CreateLogItemRequest logRequest = null;
                    try
                    {
                        var sharedMessage = JsonSerializer.Deserialize<AddLogCommunicationMessage>(message);

                        logRequest = new CreateLogItemRequest
                        {
                            Level = _logMessageLevelMap[sharedMessage.Level],
                            Time = sharedMessage.Time,
                            Text = sharedMessage.Text
                        };
                        if (sharedMessage.Attach != null)
                        {
                            logRequest.Attach = new LogItemAttach
                            {
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
                        _flowItems[id].TestReporter.Log(new CreateLogItemRequest { Level = LogLevel.Info, Time = DateTime.UtcNow, Text = message });
                    }

                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public void TestMessage(string report)
        {
            var xElement = XElement.Parse(report);

            try
            {
                var message = xElement.Value;
                var action = xElement.Attribute("destination").Value;

                switch (action)
                {
                    case LogHandler.LogMessageHandler.ReportPortal_AddLogMessage:
                        var addLogCommunicationMessage = JsonSerializer.Deserialize<AddLogCommunicationMessage>(message);
                        HandleLogCommunicationMessage(xElement, addLogCommunicationMessage);
                        break;
                    case LogHandler.LogMessageHandler.ReportPortal_BeginLogScopeMessage:
                        var beginScopeCommunicationMessage = JsonSerializer.Deserialize<BeginScopeCommunicationMessage>(message);
                        HandleBeginScopeCommunicationMessage(xElement, beginScopeCommunicationMessage);
                        break;
                    case LogHandler.LogMessageHandler.ReportPortal_EndLogScopeMessage:
                        var endScopeCommunicationAction = JsonSerializer.Deserialize<EndScopeCommunicationMessage>(message);
                        HandleEndScopeCommunicationMessage(endScopeCommunicationAction);
                        break;
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        private void HandleLogCommunicationMessage(XElement xElement, AddLogCommunicationMessage message)
        {
            var testId = xElement.Attribute("testid").Value;

            var logRequest = new CreateLogItemRequest
            {
                Level = _logMessageLevelMap[message.Level],
                Time = message.Time,
                Text = message.Text
            };

            if (message.Attach != null)
            {
                logRequest.Attach = new LogItemAttach
                {
                    MimeType = message.Attach.MimeType,
                    Data = message.Attach.Data
                };
            }

            if (message.ParentScopeId != null)
            {
                _nestedSteps[message.ParentScopeId].Log(logRequest);
            }
            else
            {
                if (message.ContextType == ContextType.Launch)
                {
                    var flowItem = FindFlowItem(testId);

                    if (flowItem.TestReporter != null)
                    {
                        flowItem.TestReporter.LaunchReporter.Log(logRequest);
                    }
                    else
                    {
                        _launchReporter.Log(logRequest);
                    }
                }
                else
                {
                    FindFlowItem(testId).TestReporter.Log(logRequest);
                }
            }
        }

        // key: id of logging scope, value: according test item reporter
        private Dictionary<string, ITestReporter> _nestedSteps = new Dictionary<string, ITestReporter>();

        private void HandleBeginScopeCommunicationMessage(XElement xElement, BeginScopeCommunicationMessage message)
        {
            var testId = xElement.Attribute("testid").Value;

            var startTestItemRequest = new StartTestItemRequest
            {
                Name = message.Name,
                StartTime = message.BeginTime,
                Type = TestItemType.Step,
                HasStats = false
            };

            ITestReporter nestedStep;

            if (message.ParentScopeId != null)
            {
                nestedStep = _nestedSteps[message.ParentScopeId].StartChildTestReporter(startTestItemRequest);
            }
            else
            {
                if (message.ContextType == ContextType.Launch)
                {
                    var flowItem = FindFlowItem(testId);

                    if (flowItem.TestReporter != null)
                    {
                        nestedStep = flowItem.TestReporter.LaunchReporter.StartChildTestReporter(startTestItemRequest);
                    }
                    else
                    {
                        nestedStep = _launchReporter?.StartChildTestReporter(startTestItemRequest);
                    }
                }
                else
                {
                    nestedStep = FindFlowItem(testId).TestReporter.StartChildTestReporter(startTestItemRequest);
                }
            }

            _nestedSteps[message.Id] = nestedStep;
        }

        private Dictionary<Shared.Execution.Logging.LogScopeStatus, Status> _nestedStepStatusMap = new Dictionary<Shared.Execution.Logging.LogScopeStatus, Status> {
            { Shared.Execution.Logging.LogScopeStatus.InProgress, Status.InProgress },
            { Shared.Execution.Logging.LogScopeStatus.Passed, Status.Passed },
            { Shared.Execution.Logging.LogScopeStatus.Failed, Status.Failed },
            { Shared.Execution.Logging.LogScopeStatus.Skipped, Status.Skipped },
            { Shared.Execution.Logging.LogScopeStatus.Warn, Status.Warn },
            { Shared.Execution.Logging.LogScopeStatus.Info, Status.Info }
        };

        private Dictionary<Shared.Execution.Logging.LogMessageLevel, LogLevel> _logMessageLevelMap = new Dictionary<Shared.Execution.Logging.LogMessageLevel, LogLevel> {
            { Shared.Execution.Logging.LogMessageLevel.Debug, LogLevel.Debug },
            { Shared.Execution.Logging.LogMessageLevel.Error, LogLevel.Error },
            { Shared.Execution.Logging.LogMessageLevel.Fatal, LogLevel.Fatal },
            { Shared.Execution.Logging.LogMessageLevel.Info, LogLevel.Info },
            { Shared.Execution.Logging.LogMessageLevel.Trace, LogLevel.Trace },
            { Shared.Execution.Logging.LogMessageLevel.Warning, LogLevel.Warning }
        };

        private void HandleEndScopeCommunicationMessage(EndScopeCommunicationMessage message)
        {
            var nestedStep = _nestedSteps[message.Id];

            nestedStep.Finish(new FinishTestItemRequest
            {
                EndTime = message.EndTime,
                Status = _nestedStepStatusMap[message.Status]
            });

            _nestedSteps.Remove(message.Id);
        }

        /// <summary>
        /// Finds some particular item in flow (suite/test/step) with fallback to parent.
        /// If the suite/test/step is ignored from sending to the server, then parent will be used (recursively.)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private FlowItemInfo FindFlowItem(string id)
        {
            var flowItem = _flowItems[id];

            if (flowItem.TestReporter != null)
            {
                return flowItem;
            }
            else
            {
                if (!string.IsNullOrEmpty(flowItem.ParentId))
                {
                    return FindFlowItem(flowItem.ParentId);
                }
                else
                {
                    return flowItem;
                }
            }
        }

        /// <summary>
        /// When strating test nunit doesn't provide info about namespace.
        /// Here we try to extrract it from full name. '(' is indicator of starting test parameters.
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        private string ExtractCodeReferenceFromFullName(string fullname)
        {
            var index = fullname.IndexOf("(");

            return index == -1 ? fullname : fullname.Substring(0, index);
        }
    }
}
