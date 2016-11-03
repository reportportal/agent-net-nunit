using NUnit.Engine;
using NUnit.Engine.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnit
{
    [Extension]
    public class ReportPortalListener : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
            Console.WriteLine(report);
        }
    }
}
