using System;
using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class BeginScopeCommunicationMessage
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string ParentScopeId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public ContextType ContextType { get; set; }
    }
}
