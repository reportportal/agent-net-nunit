using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.Xml.Linq;
using System.Xml.XPath;

using RPLogLevel = ReportPortal.Client.Abstractions.Models.LogLevel;

namespace ReportPortal.NUnitExtension.Handlers.Attributes
{
    internal class ReasonHandler : IAttributeHandler
    {
        public void Handle(XElement xElement, ITestReporter reporter)
        {
            var reasonNode = xElement.XPathSelectElement("//reason");

            if (reasonNode is null)
            {
                return;
            }

            var reasonMessage = reasonNode.XPathSelectElement("./message")?.Value;

            reporter.Log(new CreateLogItemRequest
            {
                Level = RPLogLevel.Error,
                Time = DateTime.UtcNow,
                Text = $"Reason: {reasonMessage}"
            });
        }
    }
}
