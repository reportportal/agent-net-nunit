using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReportPortal.Client.Models;

namespace ReportPortal.NUnitExtension
{
    public class BridgeExtension : IBridgeExtension
    {
        public bool Log(LogLevel level, string message)
        {
            NUnit.Framework.TestContext.Progress.WriteLine(message);
            return true;
        }
    }
}
