using ReportPortal.Client.Abstractions.Requests;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal interface IPropertyHandler
    {
        void Handle(XElement xElement, FinishTestItemRequest request);
    }
}
