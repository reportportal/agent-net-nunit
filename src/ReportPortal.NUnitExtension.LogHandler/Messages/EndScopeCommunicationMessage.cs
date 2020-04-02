using ReportPortal.Shared.Logging;
using System;
using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class EndScopeCommunicationMessage
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public LogScopeStatus Status { get; set; }
    }
}
