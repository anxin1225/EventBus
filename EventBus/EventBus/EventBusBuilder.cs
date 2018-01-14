using System;
using System.Collections.Generic;

namespace EventBusX
{
    public class EventBusBuilder
    {
        private ILogger _Logger = null;

        public List<SubscriberInfoIndex> SubscriberInfoIndexes { get; set; }
        public bool EventInheritance { get; internal set; }
        public IMainThreadSupport MainThreadSupport { get; internal set; }

        public EventBusBuilder()
        {
        }

        public EventBusBuilder InitWithLog(ILogger logger)
        {
            _Logger = logger;
            return this;
        }

        public EventBusBuilder InitWithConsoleLog()
        {
            _Logger = new ConsoleLogger();
            return this;
        }

        public ILogger GetLogger()
        {
            return _Logger;
        }
    }
}
