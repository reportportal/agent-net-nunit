using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Logging;
using System;
using System.Linq;

namespace ReportPortal.NUnitExtension.LogHandler
{
    public class LogMessageHandler : ILogHandler
    {
        public const string ReportPortal_AddLogMessage = "ReportPortal-AddLogMessage";
        public const string ReportPortal_BeginLogScopeMessage = "ReportPortal-BeginLogScopeMessage";
        public const string ReportPortal_EndLogScopeMessage = "ReportPortal-EndLogScopeMessage";

        private static ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<LogMessageHandler>();

        static LogMessageHandler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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

        public int Order => 100;

        public bool Handle(ILogScope logScope, CreateLogItemRequest logRequest)
        {
            var communicationMessage = new AddLogCommunicationMessage()
            {
                ParentScopeId = logScope?.Id,
                Time = logRequest.Time,
                Text = logRequest.Text,
                Level = logRequest.Level
            };
            if (logRequest.Attach != null)
            {
                communicationMessage.Attach = new Attach
                {
                    Name = logRequest.Attach.Name,
                    MimeType = logRequest.Attach.MimeType,
                    Data = logRequest.Attach.Data
                };
            }

            SendMessage(ReportPortal_AddLogMessage, ModelSerializer.Serialize<AddLogCommunicationMessage>(communicationMessage));

            return true;
        }

        public void BeginScope(ILogScope logScope)
        {
            var communicationMessage = new BeginScopeCommunicationMessage
            {
                Id = logScope.Id,
                ParentScopeId = logScope.Parent?.Id,
                Name = logScope.Name,
                BeginTime = logScope.BeginTime
            };

            SendMessage(ReportPortal_BeginLogScopeMessage, ModelSerializer.Serialize<BeginScopeCommunicationMessage>(communicationMessage));
        }

        public void EndScope(ILogScope logScope)
        {
            var communicationMessage = new EndScopeCommunicationMessage
            {
                Id = logScope.Id,
                EndTime = logScope.EndTime.Value,
                Status = logScope.Status
            };

            SendMessage(ReportPortal_EndLogScopeMessage, ModelSerializer.Serialize<EndScopeCommunicationMessage>(communicationMessage));
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
