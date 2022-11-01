using ReportPortal.Shared.Execution.Logging;
using System;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class EndScopeCommunicationMessage
    {
        public string Id { get; set; }

        public DateTime EndTime { get; set; }

        public LogScopeStatus Status { get; set; }
    }
}
