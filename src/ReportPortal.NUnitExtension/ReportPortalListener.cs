using NUnit.Engine;
using NUnit.Engine.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension
{
    [Extension]
    public class ReportPortalListener : ITestEventListener
    {
        public ReportPortalListener()
        {
            Console.WriteLine(Configuration.ReportPortal.Launch.Name);
        }

        public void OnTestEvent(string report)
        {
            Console.WriteLine("Message from addin ID: " + this.GetHashCode());
            Console.WriteLine(report);
        }
    }
}
