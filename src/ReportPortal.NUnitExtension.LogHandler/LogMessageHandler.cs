using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Linq;
using ReportPortal.Shared.Extensibility.Commands;
using System.Collections.Generic;
using System.Collections;
using ReportPortal.Shared.Execution.Metadata;
using System.Text.Json;

namespace ReportPortal.NUnitExtension.LogHandler
{
    public class LogMessageHandler : ICommandsListener
    {
        private const string Nunit_Category = "Category";

        public const string ReportPortal_AddLogMessage = "ReportPortal-AddLogMessage";
        public const string ReportPortal_BeginLogScopeMessage = "ReportPortal-BeginLogScopeMessage";
        public const string ReportPortal_EndLogScopeMessage = "ReportPortal-EndLogScopeMessage";

        private static ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<LogMessageHandler>();

        static LogMessageHandler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public void Initialize(ICommandsSource commandsSource)
        {
            commandsSource.OnBeginLogScopeCommand += CommandsSource_OnBeginLogScopeCommand;
            commandsSource.OnEndLogScopeCommand += CommandsSource_OnEndLogScopeCommand;
            commandsSource.OnLogMessageCommand += CommandsSource_OnLogMessageCommand;

            commandsSource.TestCommandsSource.OnGetTestAttributes += TestCommandsSource_OnGetTestAttributes;
            commandsSource.TestCommandsSource.OnAddTestAttributes += TestCommandsSource_OnAddTestAttributes;
        }

        private void TestCommandsSource_OnAddTestAttributes(Shared.Execution.ITestContext testContext, Shared.Extensibility.Commands.CommandArgs.TestAttributesCommandArgs args)
        {
            IList propertiesToAppend = new List<string>();
            foreach (var attr in args.Attributes)
            {
                if (attr.Key == Nunit_Category)
                {
                    propertiesToAppend.Add(attr.Value);
                }
                else
                {
                    propertiesToAppend.Add($"{attr.Key}:{attr.Value}");
                }
            }

            var properties = NUnit.Framework.Internal.TestExecutionContext.CurrentContext.CurrentTest.Properties;

            if (properties.ContainsKey(Nunit_Category))
            {
                var categories = properties[Nunit_Category];
                foreach (var propertyToAppend in propertiesToAppend)
                {
                    categories.Add(propertyToAppend);
                }
            }
            else
            {
                properties[Nunit_Category] = propertiesToAppend;
            }
        }

        private void TestCommandsSource_OnGetTestAttributes(Shared.Execution.ITestContext testContext, Shared.Extensibility.Commands.CommandArgs.TestAttributesCommandArgs args)
        {
            var properties = NUnit.Framework.Internal.TestExecutionContext.CurrentContext.CurrentTest.Properties;
            if (properties.ContainsKey(Nunit_Category))
            {
                var categories = properties[Nunit_Category];

                foreach (string category in categories)
                {
                    args.Attributes.Add(new MetaAttribute(Nunit_Category, category));
                }
            }
        }

        private void CommandsSource_OnLogMessageCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogMessageCommandArgs args)
        {
            var communicationMessage = new AddLogCommunicationMessage()
            {
                ParentScopeId = args.LogScope?.Id,
                Time = args.LogMessage.Time,
                Text = args.LogMessage.Message,
                Level = args.LogMessage.Level
            };

            communicationMessage.ContextType = logContext is Shared.Execution.LaunchContext ? ContextType.Launch : ContextType.Test;

            if (args.LogMessage.Attachment != null)
            {
                communicationMessage.Attach = new Attach
                {
                    MimeType = args.LogMessage.Attachment.MimeType,
                    Data = args.LogMessage.Attachment.Data
                };
            }

            SendMessage(ReportPortal_AddLogMessage, JsonSerializer.Serialize(communicationMessage));
        }

        private void CommandsSource_OnEndLogScopeCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogScopeCommandArgs args)
        {
            var communicationMessage = new EndScopeCommunicationMessage
            {
                Id = args.LogScope.Id,
                EndTime = args.LogScope.EndTime.Value,
                Status = args.LogScope.Status
            };

            SendMessage(ReportPortal_EndLogScopeMessage, JsonSerializer.Serialize(communicationMessage));
        }

        private void CommandsSource_OnBeginLogScopeCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogScopeCommandArgs args)
        {
            var communicationMessage = new BeginScopeCommunicationMessage
            {
                Id = args.LogScope.Id,
                ParentScopeId = args.LogScope.Parent?.Id,
                Name = args.LogScope.Name,
                BeginTime = args.LogScope.BeginTime
            };

            communicationMessage.ContextType = logContext is Shared.Execution.LaunchContext ? ContextType.Launch : ContextType.Test;

            SendMessage(ReportPortal_BeginLogScopeMessage, JsonSerializer.Serialize(communicationMessage));
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // since this assembly has deep link to "nunit.framework", use any already loaded "nunit.framework" assembly from test app domain

            if (args.Name.ToLowerInvariant().Contains("nunit.framework"))
            {
                return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "nunit.framework");
            }

            return null;
        }

        private void SendMessage(string command, string message)
        {
            try
            {
                NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage(command, message);
            }
            catch (Exception exp)
            {
                TraceLogger.Error($"Error while sending test communication message to nunit engine. {exp}");
            }
        }
    }
}
