using ReportPortal.Client.Abstractions.Requests;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal class RetryPropertyHandler : BasePropertyHandler
    {
        private const string Retry = "Retry";

        public RetryPropertyHandler()
            : base(Retry)
        {
        }

        public override void Handle(XElement xElement, FinishTestItemRequest request)
        {
            var isRetry = xElement.XPathSelectElement(Selector);

            request.IsRetry = isRetry != null;
        }
    }
}
