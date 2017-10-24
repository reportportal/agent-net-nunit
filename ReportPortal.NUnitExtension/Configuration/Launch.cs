using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.Configuration
{
    [DataContract]
    public class Launch
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "debugMode")]
        public bool IsDebugMode { get; set; }

        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}
