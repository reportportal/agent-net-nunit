using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Client.Converters;

namespace ReportPortal.NUnitExtension
{
    public class BridgeExtension : IBridgeExtension
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

            NUnit.Framework.TestContext.Progress.WriteLine(ModelSerializer.Serialize<SharedLogMessage>(sharedMessage));
            Handled = true;
        }
    }
}
