using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;

namespace ReportPortal.NUnitExtension.EventArguments
{
    public class TestItemStartedEventArgs: EventArgs
    {
        private readonly Service _service;
        private readonly StartTestItemRequest _request;
        private string _id;
        public TestItemStartedEventArgs(Service service, StartTestItemRequest request)
        {
            _service = service;
            _request = request;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, string id)
            :this(service, request)
        {
            _id = id;
        }

        public Service Service
        {
            get { return _service; }
        }

        public StartTestItemRequest TestItem
        {
            get { return _request; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public bool Canceled { get; set; }
    }
}
