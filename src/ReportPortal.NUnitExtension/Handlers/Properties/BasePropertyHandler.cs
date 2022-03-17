using ReportPortal.Client.Abstractions.Requests;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal abstract class BasePropertyHandler : IPropertyHandler
    {
        protected BasePropertyHandler(string property)
        {
            Property = property;
            Selector = $"//properties/property[@name='{property}']";
        }

        protected string Selector { get; }

        protected string Property { get; }

        public abstract void Handle(XElement xElement, FinishTestItemRequest request);
    }
}