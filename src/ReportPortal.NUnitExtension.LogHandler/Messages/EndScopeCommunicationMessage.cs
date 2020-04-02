using ReportPortal.Shared.Logging;
using System;
using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class EndScopeCommunicationMessage : BaseCommunicationMessage
    {
        public override CommunicationAction Action { get; set; } = CommunicationAction.EndLogScope;

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public LogScopeStatus Status { get; set; }
    }
}
