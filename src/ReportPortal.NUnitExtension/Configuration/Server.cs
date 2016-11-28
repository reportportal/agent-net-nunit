using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension.Configuration
{
    public class Server
    {
        public Uri Url { get; set; }

        public string Project { get; set; }

        public Authentication Authentication { get; set; }

        public Uri Proxy { get; set; }
    }
}
