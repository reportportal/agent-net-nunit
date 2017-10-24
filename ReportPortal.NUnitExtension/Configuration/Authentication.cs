using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension.Configuration
{
    [DataContract]
    public class Authentication
    {
        [DataMember(Name = "uuid")]
        public string Uuid { get; set; }
    }
}
