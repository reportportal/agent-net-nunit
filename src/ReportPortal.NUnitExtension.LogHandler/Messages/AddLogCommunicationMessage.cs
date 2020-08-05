using ReportPortal.Shared.Execution.Logging;
using System;
using System.Runtime.Serialization;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    /// <summary>
    /// LogHandler serializes this class to be handled later in nunit extension
    /// </summary>
    [DataContract]
    public class AddLogCommunicationMessage
    {
        [DataMember]
        public string ParentScopeId { get; set; }

        /// <summary>
        /// Date time of log item.
        /// </summary>
        [DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// A level of log item.
        /// </summary>
        [DataMember]
        public LogMessageLevel Level = LogMessageLevel.Info;

        /// <summary>
        /// Message of log item.
        /// </summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>
        /// Specify an attachment of log item.
        /// </summary>
        [DataMember]
        public Attach Attach { get; set; }
    }

    [DataContract]
    public class Attach
    {
        public Attach()
        {

        }

        public Attach(string mimeType, byte[] data)
        {
            MimeType = mimeType;
            Data = data;
        }

        [DataMember]
        public byte[] Data { get; set; }

        [DataMember]
        public string MimeType { get; set; }
    }
}
