using ReportPortal.Client.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.Shared.Extensibility;
using System;
using System.Linq;

namespace ReportPortal.NUnitExtension.BridgeExtensions
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

        public bool Handle(AddLogItemRequest logRequest)
        {
            var sharedMessage = new SharedLogMessage()
            {
                TestItemId = logRequest.TestItemId,
                Time = logRequest.Time,
                Text = logRequest.Text,
                Level = logRequest.Level
            };
            if (logRequest.Attach != null)
            {
                sharedMessage.Attach = new Attach
                {
                    Name = logRequest.Attach.Name,
                    MimeType = logRequest.Attach.MimeType,
                    Data = logRequest.Attach.Data
                };
            }

            NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<SharedLogMessage>(sharedMessage));

            return true;
        }
    }
}
