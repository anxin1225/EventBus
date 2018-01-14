using System;
namespace EventBusX
{
    public class NoSubscriberEvent
    {
        public EventBus _EventBus;

        public object _OriginalEvent;

        public NoSubscriberEvent(EventBus eventBus, Object originalEvent)
        {
            _EventBus = eventBus;
            _OriginalEvent = originalEvent;
        }
    }
}
