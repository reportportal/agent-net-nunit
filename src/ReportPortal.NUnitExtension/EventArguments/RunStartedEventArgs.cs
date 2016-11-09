using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class RunStartedEventArgs: EventArgs
    {
        private readonly Service _service;
        private readonly StartLaunchRequest _request;
        private readonly string _id;
        public RunStartedEventArgs(Service service, StartLaunchRequest request)
        {
            _service = service;
            _request = request;
        }

        public RunStartedEventArgs(Service service, StartLaunchRequest request, string id)
            :this(service, request)
        {
            _id = id;
        }

        public Service Service
        {
            get { return _service; }
        }

        public StartLaunchRequest Launch
        {
            get { return _request; }
        }

        public string Id
        {
            get { return _id; }
        }

        public bool Canceled { get; set; }
    }
}
