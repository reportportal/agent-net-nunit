using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.NUnitExtension.Extensions;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportPortal.NUnitExtension.Handlers.Properties
{
    internal class AuthorPropertyHandler : BasePropertyHandler
    {
        private const string Author = "Author";

        public AuthorPropertyHandler()
            : base(Author)
        {
        }

        public override void Handle(XElement xElement, FinishTestItemRequest request)
        {
            var authorNodes = xElement.XPathSelectElements(Selector);

            if (authorNodes == null)
            {
                return;
            }

            if (request.Attributes == null)
            {
                request.Attributes = new List<ItemAttribute>();
            }

            foreach (XElement authorNode in authorNodes)
            {
                var author = authorNode.Attribute("value").Value;

                if (author.HasValue())
                {
                    request.Attributes.Add(new ItemAttribute { Key = Property, Value = author });
                }
            }
        }
    }
}
