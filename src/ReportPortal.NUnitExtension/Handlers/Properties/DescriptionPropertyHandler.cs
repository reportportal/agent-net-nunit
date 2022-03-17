using ReportPortal.Client.Abstractions.Requests;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal class DescriptionPropertyHandler : BasePropertyHandler
    {
        private const string Description = "Description";

        public DescriptionPropertyHandler()
            : base(Description)
        {
        }

        public override void Handle(XElement xElement, FinishTestItemRequest request)
        {
            var description = xElement.XPathSelectElement(Selector);

            if (description != null)
            {
                request.Description = description.Attribute("value").Value;
            }
        }
    }
}
