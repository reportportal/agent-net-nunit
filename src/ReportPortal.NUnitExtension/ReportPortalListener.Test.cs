using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public static event EventHandler<TestItemStartedEventArgs> BeforeTestStarted;

        public static event EventHandler<TestItemStartedEventArgs> AfterTestStarted;

        public static event EventHandler<TestItemFinishedEventArgs> BeforeTestFinished;

        public static event EventHandler<TestItemFinishedEventArgs> AfterTestFinished;

        public static event EventHandler<TestItemOutputEventArgs> BeforeTestOutput;

        public static event EventHandler<TestItemOutputEventArgs> AfterTestOutput;

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

                var itemStartedEventArgs = new TestItemStartedEventArgs(_rpService, startTestRequest, null, report);
                RiseEvent(BeforeTestStarted, itemStartedEventArgs, nameof(BeforeTestStarted));

                if (!itemStartedEventArgs.Canceled)
                {
                    var testReporter = _flowItems[parentId].TestReporter.StartChildTestReporter(startTestRequest);

                    _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Test, fullname, testReporter, startTime);

                    itemStartedEventArgs.TestReporter = testReporter;
                    RiseEvent(AfterTestStarted, itemStartedEventArgs, nameof(AfterTestStarted));
                }
            }
            catch (Exception exception)
            {
                _traceLogger.Error("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

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
                        RiseEvent(BeforeTestOutput, outputEventArgs, nameof(BeforeTestOutput));

                        if (!outputEventArgs.Canceled)
                        {
                            _flowItems[id].TestReporter.Log(outputLogRequest);
                            RiseEvent(AfterTestOutput, outputEventArgs, nameof(AfterTestOutput));
                        }
                    }

                    HandleAttributes(xElement, _flowItems[id].TestReporter);

                    // finishing test
                    var finishTestRequest = new FinishTestItemRequest
                    {
                        EndTime = _flowItems[id].StartTime.AddSeconds(duration),
                        Status = _statusMap[result]
                    };

                    HandleProperties(xElement, finishTestRequest);

                    var itemFinishedEventArgs = new TestItemFinishedEventArgs(_rpService, finishTestRequest, _flowItems[id].TestReporter, report);
                    RiseEvent(BeforeTestFinished, itemFinishedEventArgs, nameof(BeforeTestFinished));

                    Action<string, FinishTestItemRequest, string, string> finishTestAction = (__id, __finishTestItemRequest, __report, __parentstacktrace) =>
                    {
                        if (__parentstacktrace.HasValue())
                        {
                            _flowItems[__id].TestReporter.Log(new CreateLogItemRequest
                            {
                                Level = LogLevel.Error,
                                Time = DateTime.UtcNow,
                                Text = __parentstacktrace
                            });
                        }

                        _flowItems[__id].TestReporter.Finish(__finishTestItemRequest);

                        var args = new TestItemFinishedEventArgs(_rpService, __finishTestItemRequest, _flowItems[__id].TestReporter, __report);
                        RiseEvent(AfterTestFinished, args, nameof(AfterTestFinished));

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
                        var sharedMessage = ModelSerializer.Deserialize<AddLogCommunicationMessage>(message);

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
                        var addLogCommunicationMessage = ModelSerializer.Deserialize<AddLogCommunicationMessage>(message);
                        HandleLogCommunicationMessage(xElement, addLogCommunicationMessage);
                        break;
                    case LogHandler.LogMessageHandler.ReportPortal_BeginLogScopeMessage:
                        var beginScopeCommunicationMessage = ModelSerializer.Deserialize<BeginScopeCommunicationMessage>(message);
                        HandleBeginScopeCommunicationMessage(xElement, beginScopeCommunicationMessage);
                        break;
                    case LogHandler.LogMessageHandler.ReportPortal_EndLogScopeMessage:
                        var endScopeCommunicationAction = ModelSerializer.Deserialize<EndScopeCommunicationMessage>(message);
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

            var testItemReporter = _flowItems[testId].TestReporter;

            if (message.ParentScopeId != null)
            {
                testItemReporter = _nestedSteps[message.ParentScopeId];
            }

            testItemReporter.Log(logRequest);
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

            var parentTestItem = _flowItems[testId].TestReporter;

            if (message.ParentScopeId != null)
            {
                parentTestItem = _nestedSteps[message.ParentScopeId];
            }

            var nestedStep = parentTestItem.StartChildTestReporter(startTestItemRequest);

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
