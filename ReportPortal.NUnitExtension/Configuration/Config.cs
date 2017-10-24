using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.Configuration
{
    [DataContract]
    public class Config
    {
        [DataMember(Name = "enabled")]
        public bool IsEnabled { get; set; }

        [DataMember(Name = "server")]
        public Server Server { get; set; }

        [DataMember(Name = "launch")]
        public Launch Launch { get; set; }
    }
}
