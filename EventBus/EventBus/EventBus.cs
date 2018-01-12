using System;
using System.Collections.Generic;
namespace EventBusX
{
    public class EventBus
    {
        private static EventBus _EventBus = null;
        private static object _lock = new object();

        private ILogger _Logger;

        private Poster MainThreadPoster;
        private Poster BackgroundPoster;
        private Poster AsyncPoster;

        private Dictionary<Type, List<Subscription>> SubscriptionsByEventType;
        private Dictionary<object, List<Type>> TypesBySubscriber;
        private Dictionary<Type, Object> StickyEvents;
        private bool EventInheritance;
        private MainThreadSupport MainThreadSupport;

        private SubscriberMethodFinder _SubscriberMethodFinder;

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
                    if (_EventBus == null)
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
            _SubscriberMethodFinder = new SubscriberMethodFinder();
            TypesBySubscriber = new Dictionary<object, List<Type>>();
            StickyEvents = new Dictionary<Type, object>();
            EventInheritance = builder.EventInheritance;
        }

        /// <summary>
        /// Register the specified subscriber.
        /// </summary>
        /// <returns>The register.</returns>
        /// <param name="subscriber">Subscriber.</param>
        public void Register(Object subscriber)
        {
            var type = subscriber.GetType();
            List<SubscriberMethod> subscriberMethods = _SubscriberMethodFinder.FindSubscriberMethods(type);
            lock (this)
            {
                foreach (var item in subscriberMethods)
                {
                    Subscribe(subscriber, item);
                }
            }
        }

        private void Subscribe(object subscriber, SubscriberMethod method)
        {
            var event_type = method.EventType;
            Subscription new_subscription = new Subscription(subscriber, method);
            List<Subscription> subscription = SubscriptionsByEventType[event_type];
            if (subscription == null)
            {
                subscription = new List<Subscription>();
                SubscriptionsByEventType[event_type] = subscription;
            }
            else
            {
                if (subscription.Contains(new_subscription))
                {
                    throw new EventBusException("Subscriber " + subscriber.GetType() + " already registered to event "
                                                + event_type);
                }
            }

            int size = subscription.Count;
            for (int i = 0; i <= size; i++)
            {
                if (i == size || method.Priority > subscription[i].SubscriberMethod.Priority)
                {
                    subscription.Insert(i, new_subscription);
                    break;
                }
            }

            List<Type> subscribed_events = TypesBySubscriber[subscriber];
            if (subscribed_events == null)
            {
                subscribed_events = new List<Type>();
                TypesBySubscriber[subscriber] = subscribed_events;
            }

            subscribed_events.Add(event_type);

            if (method.Sticky)
            {
                if (EventInheritance)
                {
                    HashSet<KeyValuePair<Type, object>> entries =
                        new HashSet<KeyValuePair<Type, object>>(StickyEvents);
                    foreach (var item in entries)
                    {
                        var candidate_type = item.Key;
                        if (event_type.IsAssignableFrom(candidate_type))
                        {
                            object sticky_event = item.Value;
                            CheckPostStickyEventToSubscription(new_subscription, sticky_event);
                        }
                    }
                }
                else
                {
                    object sticky_event = StickyEvents[event_type];
                    CheckPostStickyEventToSubscription(new_subscription, sticky_event);
                }
            }
        }

        private void CheckPostStickyEventToSubscription(Subscription newSubscription, Object stickyEvent)
        {
            if (stickyEvent != null)
            {
                // If the subscriber is trying to abort the event, it will fail (event is not tracked in posting state)
                // --> Strange corner case, which we don't take care of here.
                PostToSubscription(newSubscription, stickyEvent, IsMainThread());
            }
        }

        private bool IsMainThread()
        {
            return MainThreadSupport != null ? MainThreadSupport.IsMainThread() : true;
        }

        private void PostToSubscription(Subscription subscription, Object event_obj, bool isMainThread)
        {
            switch (subscription.SubscriberMethod.ThreadMode)
            {
                case ThreadMode.POSTING:
                    InvokeSubscriber(subscription, event_obj);
                    break;

                case ThreadMode.MAIN:
                    if (isMainThread)
                    {
                        InvokeSubscriber(subscription, event_obj);
                    }
                    else
                    {
                        MainThreadPoster.Enqueue(subscription, event_obj);
                    }
                    break;

                case ThreadMode.MAIN_ORDERED:
                    if (MainThreadPoster != null)
                    {
                        MainThreadPoster.Enqueue(subscription, event_obj);
                    }
                    else
                    {
                        InvokeSubscriber(subscription, event_obj);
                    }
                    break;

                case ThreadMode.BACKGROUND:
                    if (isMainThread)
                    {
                        BackgroundPoster.Enqueue(subscription, event_obj);
                    }
                    else
                    {
                        InvokeSubscriber(subscription, event_obj);
                    }
                    break;

                case ThreadMode.ASYNC:
                    AsyncPoster.Enqueue(subscription, event_obj);
                    break;
                default:
                    break;
            }
        }

        public void InvokeSubscriber(PendingPost pendingPost)
        {
            object event_obj = pendingPost.Event;
            Subscription subscription = pendingPost.Subscription;
            PendingPost.ReleasePendingPost(pendingPost);
            if (subscription.Active)
            {
                InvokeSubscriber(subscription, event_obj);
            }
        }
        public void InvokeSubscriber(Subscription subscription, Object event_obj)
        {
            try
            {
                subscription.SubscriberMethod.Method.Invoke(subscription.Subscriber, event_obj);
            }
            //catch (InvocationTargetException e)
            //{
            //    handleSubscriberException(subscription, event_obj, e.getCause());
            //}
            //catch (IllegalAccessException e)
            //{
            //    throw new IllegalStateException("Unexpected exception", e);
            //}
            catch (Exception ex)
            {
                throw new NotImplementedException();
            }
        }
    }
}
