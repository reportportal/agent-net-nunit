using ReportPortal.Shared.Reporter;
using System.Xml.Linq;

namespace ReportPortal.NUnitExtension.Handlers.Attributes
{
    internal interface IAttributeHandler
    {
        void Handle(XElement xElement, ITestReporter reporter);
    }
}
