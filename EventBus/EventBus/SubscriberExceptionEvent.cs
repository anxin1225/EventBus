using System;
namespace EventBusX
{
    public class SubscriberExceptionEvent
    {
        public EventBus EventBus { get; set; }

        public Exception Exception { get; set; }

        public object CausingEvent { get; set; }

        public object CausingSubscriber { get; set; }

        public SubscriberExceptionEvent(EventBus eventBus, Exception exception, Object causingEvent,
            Object causingSubscriber)
        {
            EventBus = eventBus;
            Exception = exception;
            CausingEvent = causingEvent;
            CausingSubscriber = causingSubscriber;
        }
    }
}
