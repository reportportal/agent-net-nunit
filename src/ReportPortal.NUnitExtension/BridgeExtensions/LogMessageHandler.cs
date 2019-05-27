using ReportPortal.Client.Requests;
using ReportPortal.Client.Converters;
using ReportPortal.Shared.Extensibility;

namespace ReportPortal.NUnitExtension.BridgeExtensions
{
    public class LogMessageHandler : ILogHandler
    {
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
