using ReportPortal.Client.Abstractions;
using System;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public abstract class BaseNUnitEventArgs : EventArgs
    {
        protected BaseNUnitEventArgs(IClientService service, string report = null)
        {
            Service = service;
            Report = report;
        }

        public IClientService Service { get; }

        public string Report { get; }

        public bool Canceled { get; set; }
    }
}
