using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.NUnitExtension.LogHandler.Messages;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Logging;
using System;
using System.Linq;

namespace ReportPortal.NUnitExtension.LogHandler
{
    public class LogMessageHandler : ILogHandler
    {
        static LogMessageHandler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // since this assembly has deep link to "nunit.framework", use any already loaded "nunit.framework" assembly from test app domain

            if (args.Name.Contains("nunit.framework"))
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

            NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<AddLogCommunicationMessage>(communicationMessage));

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

            NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<BeginScopeCommunicationMessage>(communicationMessage));
        }

        public void EndScope(ILogScope logScope)
        {
            var communicationMessage = new EndScopeCommunicationMessage
            {
                Id = logScope.Id,
                EndTime = logScope.EndTime.Value,
                Status = logScope.Status
            };

            NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<EndScopeCommunicationMessage>(communicationMessage));

        }
    }
}
