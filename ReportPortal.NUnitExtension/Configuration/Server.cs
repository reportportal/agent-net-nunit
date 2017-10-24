using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension.Configuration
{
    [DataContract]
    public class Server
    {
        [DataMember(Name = "url")]
        public Uri Url { get; set; }

        [DataMember(Name = "project")]
        public string Project { get; set; }

        [DataMember(Name = "authentication")]
        public Authentication Authentication { get; set; }

        [DataMember(Name = "proxy")]
        public Uri Proxy { get; set; }
    }
}
