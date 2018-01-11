using System;
using System.Collections.Generic;
namespace EventBusX
{
    public class EventBus
    {
        private static EventBus _EventBus = null;
        private static object _lock = new object();

        private ILogger _Logger;

        private Dictionary<Type, List<Subscription>> subscriptionsByEventType;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        public static EventBus GetInstance()
        {
            if (_EventBus == null)
            {
                lock (_lock)
                {
                    if(_EventBus == null)
                    {
                        _EventBus = new EventBus();
                    }
                }
            }

            return _EventBus;
        }

        /// <summary>
        /// Gets the builder.
        /// </summary>
        /// <returns>The builder.</returns>
        public static EventBusBuilder Builder() { return new EventBusBuilder(); }

        /// <summary>
        /// Clears the caches.
        /// </summary>
        public static void ClearCaches()
        {
            throw new NotImplementedException();
        }

        public EventBus()
            : this(new EventBusBuilder())
        {
        }

        public EventBus(EventBusBuilder builder)
        {
            _Logger = builder.GetLogger();
        }

        /// <summary>
        /// Register the specified subscriber.
        /// </summary>
        /// <returns>The register.</returns>
        /// <param name="subscriber">Subscriber.</param>
        public void Register(Object subscriber)
        {
            var type = subscriber.GetType();
            List<SubscriberMethod> subscriberMethods = subscriberMethodFinder.findSubscriberMethods(subscriberClass);
        }
    }
}
