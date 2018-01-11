using System;
namespace EventBusX
{
    public delegate void SubscriberMethodDelegate(Subscription sub, object obj);

    /// <summary>
    /// Subscriber method.
    /// </summary>
    public class SubscriberMethod
    {
        public SubscriberMethodDelegate Method { get; private set; }

        public ThreadMode ThreadMode { get; private set; }

        public Type EventType { get; private set; }

        public int Priority { get; private set; }

        public bool Sticky { get; private set; }

        public SubscriberMethod(SubscriberMethodDelegate method, ThreadMode model, Type type, int priority, bool sticky)
        {
            Method = method;
            ThreadMode = model;
            EventType = type;
            Priority = priority;
            Sticky = sticky;
        }

        public override int GetHashCode()
        {
            return Method?.GetHashCode() ?? 0;
        }
    }
}
