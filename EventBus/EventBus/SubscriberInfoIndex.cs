using System;
namespace EventBusX
{
    public interface SubscriberInfoIndex
    {
        SubscriberInfo GetSubscriberInfo(Type type);
    }
}
