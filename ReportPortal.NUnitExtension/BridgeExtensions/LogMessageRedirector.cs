using ReportPortal.Shared;
using ReportPortal.Client.Requests;
using ReportPortal.Client.Converters;

namespace ReportPortal.NUnitExtension.BridgeExtensions
{
    public class LogMessageRedirector : IBridgeExtension
    {
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

            NUnit.Framework.Internal.TestExecutionContext.CurrentContext.SendMessage("ReportPortal", ModelSerializer.Serialize<SharedLogMessage>(sharedMessage));

            Handled = true;
        }
    }
}
