using System;
namespace EventBusX
{
    public interface SubscriberInfo
    {
        Type GetSubscriberClass();

        SubscriberMethod[] GetSubscriberMethods();

        SubscriberInfo GetSuperSubscriberInfo();

        bool ShouldCheckSuperclass();
    }
}
