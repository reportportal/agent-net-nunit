using NUnit.Engine;
using NUnit.Engine.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitAddin
{
    [Extension]
    public class ReportPortalListener : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
            Console.WriteLine("Message from addin ID: " + this.GetHashCode());
            Console.WriteLine(report);
        }
    }
}
