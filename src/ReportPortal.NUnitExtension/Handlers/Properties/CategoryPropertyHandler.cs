using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Extensions;
using ReportPortal.Shared.Converters;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal class CategoryPropertyHandler : BasePropertyHandler
    {
        private const string Category = "Category";

        public CategoryPropertyHandler()
            : base(Category)
        {
        }

        public override void Handle(XElement xElement, FinishTestItemRequest request)
        {
            var categoryNodes = xElement.XPathSelectElements(Selector);

            if (categoryNodes is null)
            {
                return;
            }

            if (request.Attributes is null)
            {
                request.Attributes = new List<ItemAttribute>();
            }

            foreach (XElement categoryNode in categoryNodes)
            {
                var category = categoryNode.Attribute("value").Value;

                if (category.HasValue())
                {
                    var attr = new ItemAttributeConverter()
                        .ConvertFrom(category, opts => opts.UndefinedKey = Property);

                    request.Attributes.Add(attr);
                }
            }
        }
    }
}
