using ReportPortal.Shared;
using System;
using System.Linq;
using ReportPortal.Client.Requests;
using ReportPortal.Client.Converters;
using System.Reflection;

namespace ReportPortal.NUnitExtension.BridgeExtensions
{
    public class LogMessageRedirector : IBridgeExtension
    {
        private Assembly _nunitFrameworkAssembly;
        private Type _testExecutionContextType;

        public bool Handled { get; set; }

        public int Order => int.MaxValue;

        public void FormatLog(ref AddLogItemRequest logRequest)
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

            // NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<SharedLogMessage>(sharedMessage));
            // Use implementation via reflection to not be dependent on NUnit framework package, find loaded classes in AppDomain

            var serializedSharedMessage = ModelSerializer.Serialize<SharedLogMessage>(sharedMessage);

            if (_nunitFrameworkAssembly == null)
            {
                _nunitFrameworkAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.ToLowerInvariant() == "nunit.framework");
                if (_nunitFrameworkAssembly == null)
                {
                    throw new Exception("NUnit Framework assembly is not loaded into current AppDomain.");
                }

                _testExecutionContextType = _nunitFrameworkAssembly.GetType("NUnit.Framework.Internal.TestExecutionContext");
            }

            var currentContextPropertyInfo = _testExecutionContextType.GetProperty("CurrentContext", BindingFlags.Public | BindingFlags.Static);
            if (currentContextPropertyInfo == null)
            {
                throw new Exception($"Cannot find static 'CurrentContext' property in {_testExecutionContextType.FullName} type.");
            }

            var currentContext = currentContextPropertyInfo.GetValue(null);

            var sendMessageMethodInfo = _testExecutionContextType.GetMethod("SendMessage");
            if (sendMessageMethodInfo == null)
            {
                throw new Exception($"Cannot find 'SendMessage' method in '{_testExecutionContextType.FullName}' type. Make sure you use NUnit v3.11+.");
            }

            sendMessageMethodInfo.Invoke(currentContext, new object[] { "ReportPortal", serializedSharedMessage });

            Handled = true;
        }
    }
}
