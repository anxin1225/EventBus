using System;
using System.Reflection;

namespace EventBusX
{
    /// <summary>
    /// Subscriber method.
    /// </summary>
    public class SubscriberMethod
    {
        public MethodInfo Method { get; private set; }

        public ThreadMode ThreadMode { get; private set; }

        public Type EventType { get; private set; }

        public int Priority { get; private set; }

        public bool Sticky { get; private set; }

        public SubscriberMethod(MethodInfo method, Type type, ThreadMode model, int priority, bool sticky)
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
