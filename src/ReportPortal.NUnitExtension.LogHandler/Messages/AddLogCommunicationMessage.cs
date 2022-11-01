using ReportPortal.Shared.Execution.Logging;
using System;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    /// <summary>
    /// LogHandler serializes this class to be handled later in nunit extension
    /// </summary>
    public class AddLogCommunicationMessage
    {
        public string ParentScopeId { get; set; }

        /// <summary>
        /// Date time of log item.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// A level of log item.
        /// </summary>
        public LogMessageLevel Level { get; set; } = LogMessageLevel.Info;

        /// <summary>
        /// Message of log item.
        /// </summary>
        public string Text { get; set; }

        public ContextType ContextType { get; set; }

        /// <summary>
        /// Specify an attachment of log item.
        /// </summary>
        public Attach Attach { get; set; }
    }

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

        public byte[] Data { get; set; }

        public string MimeType { get; set; }
    }
}
