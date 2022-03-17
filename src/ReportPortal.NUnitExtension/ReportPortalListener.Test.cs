using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.NUnitExtension.Attributes;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Execution.Logging;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

        private static readonly Dictionary<LogScopeStatus, Status> _nestedStepStatusMap = new Dictionary<LogScopeStatus, Status> {
            { LogScopeStatus.InProgress, Status.InProgress },
            { LogScopeStatus.Passed, Status.Passed },
            { LogScopeStatus.Failed, Status.Failed },
            { LogScopeStatus.Skipped, Status.Skipped },
            { LogScopeStatus.Warn, Status.Warn },
            { LogScopeStatus.Info, Status.Info }
        };

        private static readonly Dictionary<LogMessageLevel, LogLevel> _logMessageLevelMap = new Dictionary<LogMessageLevel, LogLevel> {
            { LogMessageLevel.Debug, LogLevel.Debug },
            { LogMessageLevel.Error, LogLevel.Error },
            { LogMessageLevel.Fatal, LogLevel.Fatal },
            { LogMessageLevel.Info, LogLevel.Info },
            { LogMessageLevel.Trace, LogLevel.Trace },
            { LogMessageLevel.Warning, LogLevel.Warning }
        };

        // key: id of logging scope, value: according test item reporter
        private readonly Dictionary<string, ITestReporter> _nestedSteps = new Dictionary<string, ITestReporter>();

        [ReportKey("<start-test")]
        public void StartTest(string report) => InvokeSafely(() => StartTest(XElement.Parse(report)));

        private void StartTest(XElement xElement)
        {
            var id = xElement.Attribute("id").Value;
            var parentId = xElement.Attribute("parentId").Value;
            var name = xElement.Attribute("name").Value;
            var fullname = xElement.Attribute("fullname").Value;

            var report = xElement.ToString();
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

            if (itemStartedEventArgs.Canceled)
            {
                return;
            }

            var reporter = _flowItems[parentId].TestReporter.StartChildTestReporter(startTestRequest);
            _flowItems[id] = new FlowItemInfo(id, parentId, FlowItemInfo.FlowType.Test, fullname, reporter, startTime);

            itemStartedEventArgs.TestReporter = reporter;
            RiseEvent(AfterTestStarted, itemStartedEventArgs, nameof(AfterTestStarted));
        }

        [ReportKey("<test-case")]
        public void FinishTest(string report) => InvokeSafely(() => FinishTest(XElement.Parse(report)));

        private void FinishTest(XElement xElement)
        {
            var id = xElement.Attribute("id").Value;
            var result = xElement.Attribute("result").Value;
            var parentId = xElement.Attribute("parentId");
            var duration = float.Parse(xElement.Attribute("duration").Value, CultureInfo.InvariantCulture);

            var report = xElement.ToString();

            if (!_flowItems.ContainsKey(id))
            {
                StartTest(report);
            }

            if (!_flowItems.ContainsKey(id))
            {
                return;
            }

            var reporter = _flowItems[id].TestReporter;

            LogOutput(reporter, xElement);
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

            /* 
                Understand whether finishing test item should be deferred.
                Usually we need it to report stacktrace in case of OneTimeSetup method fails, and stacktrace is avalable later in "FinishSuite" method
            */
            if (IsShouldBeDeferred(xElement))
            {
                _flowItems[id].FinishTestItemRequest = finishTestRequest;
                _flowItems[id].Report = report;
                _flowItems[id].DeferredFinishAction = GetFinishTestAction();
            }
            else
            {
                GetFinishTestAction().Invoke(id, finishTestRequest, report, null);
            }
        }

        private void LogOutput(ITestReporter reporter, XElement xElement)
        {
            var report = xElement.ToString();
            var outputNode = xElement.XPathSelectElement("//output");

            if (outputNode is null)
            {
                return;
            }

            var outputLogRequest = new CreateLogItemRequest
            {
                Level = LogLevel.Trace,
                Time = DateTime.UtcNow,
                Text = "Test Output: " + Environment.NewLine + outputNode.Value
            };

            var outputEventArgs = new TestItemOutputEventArgs(_rpService, outputLogRequest, reporter, report);
            RiseEvent(BeforeTestOutput, outputEventArgs, nameof(BeforeTestOutput));

            if (outputEventArgs.Canceled)
            {
                return;
            }

            reporter.Log(outputLogRequest);
            RiseEvent(AfterTestOutput, outputEventArgs, nameof(AfterTestOutput));
        }

        [ReportKey("<test-output")]
        public void TestOutput(string report) => InvokeSafely(() => TestOutput(XElement.Parse(report)));

        private void TestOutput(XElement xElement)
        {
            var id = xElement.Attribute("testid").Value;
            var message = xElement.Value;

            if (!_flowItems.ContainsKey(id))
            {
                return;
            }

            TryGetCreateLogItemRequest(message, out var request);

            request = request ?? new CreateLogItemRequest
            {
                Level = LogLevel.Info,
                Time = DateTime.UtcNow,
                Text = message
            };

            _flowItems[id].TestReporter.Log(request);
        }

        [ReportKey("<test-message")]
        public void TestMessage(string report) => InvokeSafely(() => TestMessage(XElement.Parse(report)));

        private void TestMessage(XElement xElement)
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

        private static bool TryGetCreateLogItemRequest(string message, out CreateLogItemRequest request)
        {
            try
            {
                var sharedMessage = ModelSerializer.Deserialize<AddLogCommunicationMessage>(message);

                request = new CreateLogItemRequest
                {
                    Level = _logMessageLevelMap[sharedMessage.Level],
                    Time = sharedMessage.Time,
                    Text = sharedMessage.Text
                };

                if (sharedMessage.Attach != null)
                {
                    request.Attach = new LogItemAttach
                    {
                        MimeType = sharedMessage.Attach.MimeType,
                        Data = sharedMessage.Attach.Data
                    };
                }
            }
            catch
            {
                request = null;
            }

            return request != null;
        }

        private Action<string, FinishTestItemRequest, string, string> GetFinishTestAction()
        {
            return (id, request, report, stacktrace) =>
            {
                var reporter = _flowItems[id].TestReporter;

                if (stacktrace.HasValue())
                {
                    reporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Error,
                        Time = DateTime.UtcNow,
                        Text = stacktrace
                    });
                }

                reporter.Finish(request);

                var eventArgs = new TestItemFinishedEventArgs(_rpService, request, reporter, report);
                RiseEvent(AfterTestFinished, eventArgs, nameof(AfterTestFinished));

                _flowItems.Remove(id);
            };
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
