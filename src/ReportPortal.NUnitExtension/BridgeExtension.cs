using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using System.Web.Script.Serialization;

namespace ReportPortal.NUnitExtension
{
    public class BridgeExtension : IBridgeExtension
    {
        public bool Handled { get; set; }

        public int Order => int.MaxValue;

        public void FormatLog(ref AddLogItemRequest logRequest)
        {
            var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            NUnit.Framework.TestContext.Progress.WriteLine(serializer.Serialize(logRequest));
            Handled = true;
        }
    }
}
