using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.Shared.Reporter;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

using RPLogLevel = ReportPortal.Client.Abstractions.Models.LogLevel;

namespace ReportPortal.NUnitExtension.Handlers.Attributes
{
    internal class FailureHandler : IAttributeHandler
    {
        public void Handle(XElement xElement, ITestReporter reporter)
        {
            var failureNode = xElement.XPathSelectElement("//failure");

            if (failureNode is null)
            {
                return;
            }

            var failureMessage = failureNode.XPathSelectElement("./message")?.Value;
            var failureStacktrace = failureNode.XPathSelectElement("./stack-trace")?.Value;

            reporter.Log(GetFailureLogItemRequest(failureMessage, failureStacktrace));

            // walk through assertions
            foreach (var node in xElement.XPathSelectElements("test-case/assertions/assertion"))
            {
                var assertionMessage = node.XPathSelectElement("message")?.Value;
                var assertionStacktrace = node.XPathSelectElement("stack-trace")?.Value;

                if (assertionMessage != failureMessage && assertionStacktrace != failureStacktrace)
                {
                    reporter.Log(GetFailureLogItemRequest(assertionMessage, assertionStacktrace));
                }
            }
        }

        private static CreateLogItemRequest GetFailureLogItemRequest(string message, string stacktrace)
        {
            string[] messages = new[] { message, stacktrace }.Where(m => m.HasValue()).ToArray();

            return new CreateLogItemRequest
            {
                Level = RPLogLevel.Error,
                Time = DateTime.UtcNow,
                Text = string.Join(Environment.NewLine, messages)
            };
        }
    }
}
