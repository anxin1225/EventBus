using System;
namespace EventBusX
{
    public class Subscription
    {
        public object Subscriber { get; set; }

        public SubscriberMethod SubscriberMethod { get; set; }

        public bool Active { get; set; }

        public Subscription(object subscriber, SubscriberMethod method)
        {
            Subscriber = subscriber;
            SubscriberMethod = method;
            Active = true;
        }

        public override int GetHashCode()
        {
            return SubscriberMethod?.GetHashCode() ?? 0;
        }
    }
}
