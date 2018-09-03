using ReportPortal.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension
{
    [DataContract]
    class SharedLogMessage
    {
        /// <summary>
        /// ID of test item to add new logs.
        /// </summary>
        [DataMember]
        public string TestItemId { get; set; }

        /// <summary>
        /// Date time of log item.
        /// </summary>
        [DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// A level of log item.
        /// </summary>
        [DataMember]
        public LogLevel Level = LogLevel.Info;

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

        public Attach(string name, string mimeType, byte[] data)
        {
            Name = name;
            MimeType = mimeType;
            Data = data;
        }

        [DataMember]
        public string Name { get; set; }

        [IgnoreDataMember]
        public byte[] Data { get; set; }

        [IgnoreDataMember]
        public string MimeType { get; set; }
    }
}
