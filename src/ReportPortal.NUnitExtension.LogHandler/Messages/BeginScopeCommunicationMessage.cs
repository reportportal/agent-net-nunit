using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class BeginScopeCommunicationMessage : BaseCommunicationMessage
    {
        public override CommunicationAction Action { get; set; } = CommunicationAction.BeginLogScope;

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string ParentScopeId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }
    }
}
