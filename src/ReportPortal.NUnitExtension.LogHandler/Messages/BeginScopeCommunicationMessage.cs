using System;

namespace ReportPortal.NUnitExtension.LogHandler.Messages
{
    public class BeginScopeCommunicationMessage
    {
        public string Id { get; set; }

        public string ParentScopeId { get; set; }

        public string Name { get; set; }

        public DateTime BeginTime { get; set; }

        public ContextType ContextType { get; set; }
    }
}
