using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    [DataContract]
    public class BaseCommunicationMessage
    {
        [DataMember(IsRequired = true)]
        public virtual CommunicationAction Action { get; set; }
    }
}
