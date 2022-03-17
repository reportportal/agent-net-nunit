using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

using RPLogLevel = ReportPortal.Client.Abstractions.Models.LogLevel;

namespace ReportPortal.NUnitExtension.Handlers.Attributes
{
    internal class AttachmentsHandler : IAttributeHandler
    {
        public void Handle(XElement xElement, ITestReporter reporter)
        {
            var attachmentNodes = xElement.XPathSelectElements("//attachments/attachment");

            foreach (var attachmentNode in attachmentNodes)
            {
                reporter.Log(ExtractCreateLogItemRequest(attachmentNode));
            }
        }

        private static CreateLogItemRequest ExtractCreateLogItemRequest(XElement xElement)
        {
            var filePath = xElement.XPathSelectElement("./filePath").Value;
            var fileDescription = xElement.XPathSelectElement("./description")?.Value;

            return File.Exists(filePath)
                ? GetRequestWithAttachment(filePath, fileDescription ?? Path.GetFileName(filePath))
                : new CreateLogItemRequest
                {
                    Level = RPLogLevel.Warning,
                    Time = DateTime.UtcNow,
                    Text = $"Attachment file '{filePath}' doesn't exists."
                };
        }

        private static CreateLogItemRequest GetRequestWithAttachment(string filePath, string text)
        {
            CreateLogItemRequest logItemRequest;

            try
            {
                logItemRequest = new CreateLogItemRequest
                {
                    Level = RPLogLevel.Info,
                    Time = DateTime.UtcNow,
                    Text = text,
                    Attach = new LogItemAttach
                    {
                        Name = Path.GetFileName(filePath),
                        MimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(filePath)),
                        Data = ToByteArray(filePath)
                    }
                };
            }
            catch (Exception ex)
            {
                logItemRequest = new CreateLogItemRequest
                {
                    Level = RPLogLevel.Warning,
                    Time = DateTime.UtcNow,
                    Text = $"Cannot read '{filePath}' file: {ex}"
                };
            }

            return logItemRequest;
        }

        private static byte[] ToByteArray(string filePath)
        {
            byte[] bytes;

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }
            }

            return bytes;
        }
    }
}
