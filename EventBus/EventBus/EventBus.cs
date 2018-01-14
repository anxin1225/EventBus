using System;
using System.Collections.Generic;
using System.Threading;

namespace EventBusX
{
    public class EventBus
    {
        private static EventBus _EventBus = null;
        private static object _lock = new object();

        private int _IndexCount;
        private ILogger _Logger;

        private IPoster _MainThreadPoster;
        private IPoster _BackgroundPoster;
        private IPoster _AsyncPoster;

        private bool _LogSubscriberExceptions = false;
        private bool _ThrowSubscriberException = false;
        private bool _LogNoSubscriberMessages = false;
        private bool _SendSubscriberExceptionEvent = false;
        private bool _SendNoSubscriberEvent = false;
        private bool _EventInheritance;

        private ThreadLock<PostingThreadState> _CurrentPostingThreadState =
            new ThreadLock<PostingThreadState>();

        private Dictionary<Type, List<Subscription>> _SubscriptionsByEventType = null;
        private Dictionary<object, List<Type>> _TypesBySubscriber;
        private Dictionary<Type, Object> _StickyEvents;
        private IMainThreadSupport _MainThreadSupport = null;

        private static Dictionary<Type, List<Type>> _EventTypesCache =
            new Dictionary<Type, List<Type>>();

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
            _SubscriptionsByEventType = new Dictionary<Type, List<Subscription>>();
            _TypesBySubscriber = new Dictionary<object, List<Type>>();
            _StickyEvents = new Dictionary<Type, object>();

            _MainThreadSupport = builder.MainThreadSupport;
            _MainThreadPoster = _MainThreadSupport?.CreatePoster(this);
            _BackgroundPoster = new BackgroundPoster(this);
            _AsyncPoster = new AsyncPoster(this);

            _IndexCount = builder.SubscriberInfoIndexes?.Count ?? 0;
            //_SubscriberMethodFinder = new SubscriberMethodFinder(builder.SubscriberInfoIndexes);

            _EventInheritance = builder.EventInheritance;
        }

        /// <summary>
        /// Register the specified subscriber.
        /// </summary>
        /// <returns>The register.</returns>
        /// <param name="subscriber">Subscriber.</param>
        private void Register(Object subscriber)
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
            List<Subscription> subscription = _SubscriptionsByEventType[event_type];
            if (subscription == null)
            {
                subscription = new List<Subscription>();
                _SubscriptionsByEventType[event_type] = subscription;
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

            List<Type> subscribed_events = _TypesBySubscriber[subscriber];
            if (subscribed_events == null)
            {
                subscribed_events = new List<Type>();
                _TypesBySubscriber[subscriber] = subscribed_events;
            }

            subscribed_events.Add(event_type);

            if (method.Sticky)
            {
                if (_EventInheritance)
                {
                    HashSet<KeyValuePair<Type, object>> entries =
                        new HashSet<KeyValuePair<Type, object>>(_StickyEvents);
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
                    object sticky_event = _StickyEvents[event_type];
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
            //return _MainThreadSupport != null ? MainThreadSupport.IsMainThread() : true;
            return _MainThreadSupport?.IsMainThread() ?? true;
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
                        _MainThreadPoster.Enqueue(subscription, event_obj);
                    }
                    break;

                case ThreadMode.MAIN_ORDERED:
                    if (_MainThreadPoster != null)
                    {
                        _MainThreadPoster.Enqueue(subscription, event_obj);
                    }
                    else
                    {
                        InvokeSubscriber(subscription, event_obj);
                    }
                    break;

                case ThreadMode.BACKGROUND:
                    if (isMainThread)
                    {
                        _BackgroundPoster.Enqueue(subscription, event_obj);
                    }
                    else
                    {
                        InvokeSubscriber(subscription, event_obj);
                    }
                    break;

                case ThreadMode.ASYNC:
                    _AsyncPoster.Enqueue(subscription, event_obj);
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
                subscription.SubscriberMethod.Method.Invoke(subscription.Subscriber, new[] { event_obj });
            }
            catch 
            {
                throw new NotImplementedException();
            }
        }

        private void HandleSubscriberException(Subscription subscription, Object event_obj, Exception ex)
        {
            if (event_obj is SubscriberExceptionEvent)
            {
                if (_LogSubscriberExceptions)
                {
                    _Logger.Log(Level.SEVERE, "SubscriberExceptionEvent subscriber " + subscription.Subscriber.GetType()
                                + " threw an exception", ex);

                    SubscriberExceptionEvent exEvent = (SubscriberExceptionEvent)event_obj;
                    _Logger.Log(Level.SEVERE, "Initial event " + exEvent.CausingEvent + " caused exception in "
                               + exEvent.CausingSubscriber, exEvent.Exception);
                }
            }
            else
            {
                if (_ThrowSubscriberException)
                {
                    throw new EventBusException("Invoking subscriber failed", ex);
                }
                if (_LogSubscriberExceptions)
                {
                    _Logger.Log(Level.SEVERE, "Could not dispatch event: " + event_obj.GetType() + " to subscribing class "
                                + subscription.Subscriber.GetType(), ex);
                }
                if (_SendSubscriberExceptionEvent)
                {
                    SubscriberExceptionEvent ex_event = new SubscriberExceptionEvent(this, ex, event_obj, subscription.Subscriber);
                    Post(ex_event);
                }
            }
        }

        /** Posts the given event to the event bus. */
        public void Post(Object event_obj)
        {
            PostingThreadState postingState = _CurrentPostingThreadState.Data;
            List<Object> eventQueue = postingState.EventQueue;
            eventQueue.Add(event_obj);

            if (!postingState.IsPosting)
            {
                postingState.IsMainThread = IsMainThread();
                postingState.IsPosting = true;
                if (postingState.Canceled)
                {
                    throw new EventBusException("Internal error. Abort state was not reset");
                }
                try
                {
                    while (eventQueue.Count != 0)
                    {
                        var item = eventQueue[0];
                        eventQueue.RemoveAt(0);
                        PostSingleEvent(item, postingState);
                    }
                }
                finally
                {
                    postingState.IsPosting = false;
                    postingState.IsMainThread = false;
                }
            }
        }

        private void PostSingleEvent(Object event_obj, PostingThreadState postingState)
        {
            var event_type = event_obj.GetType();
            bool subscriptionFound = false;
            if (_EventInheritance)
            {
                List<Type> eventTypes = LookupAllEventTypes(event_type);
                int countTypes = eventTypes.Count;
                for (int h = 0; h < countTypes; h++)
                {
                    Type clazz = eventTypes[h];
                    subscriptionFound |= PostSingleEventForEventType(event_obj, postingState, clazz);
                }
            }
            else
            {
                subscriptionFound = PostSingleEventForEventType(event_obj, postingState, event_type);
            }
            if (!subscriptionFound)
            {
                if (_LogNoSubscriberMessages)
                {
                    _Logger.Log(Level.INFO, "No subscribers registered for event " + event_type);
                }
                if (_SendNoSubscriberEvent && event_type != typeof(NoSubscriberEvent) &&
                    event_type != typeof(SubscriberExceptionEvent))
                {
                    Post(new NoSubscriberEvent(this, event_obj));
                }
            }
        }

        private bool PostSingleEventForEventType(Object event_obj, PostingThreadState postingState, Type eventClass)
        {
            List<Subscription> subscriptions;
            lock (this)
            {
                subscriptions = _SubscriptionsByEventType[eventClass];
            }
            if (subscriptions != null && subscriptions.Count != 0)
            {
                foreach (var subscription in subscriptions)
                {
                    postingState.Event = event_obj;
                    postingState.Subscription = subscription;
                    bool aborted = false;
                    try
                    {
                        PostToSubscription(subscription, event_obj, postingState.IsMainThread);
                        aborted = postingState.Canceled;
                    }
                    finally
                    {
                        postingState.Event = null;
                        postingState.Subscription = null;
                        postingState.Canceled = false;
                    }
                    if (aborted)
                    {
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        private static List<Type> LookupAllEventTypes(Type eventClass)
        {
            lock (_EventTypesCache)
            {
                _EventTypesCache.TryGetValue(eventClass, out List<Type> eventTypes);
                if (eventTypes == null)
                {
                    eventTypes = new List<Type>();
                    Type clazz = eventClass;
                    while (clazz != null)
                    {
                        eventTypes.Add(clazz);
                        AddInterfaces(eventTypes, clazz.GetInterfaces());
                        clazz = clazz.BaseType;
                    }
                    _EventTypesCache[eventClass] = eventTypes;
                }
                return eventTypes;
            }
        }

        static void AddInterfaces(List<Type> eventTypes, Type[] interfaces)
        {
            foreach (var interfaceClass in interfaces)
            {
                if (!eventTypes.Contains(interfaceClass))
                {
                    eventTypes.Add(interfaceClass);
                    AddInterfaces(eventTypes, interfaceClass.GetInterfaces());
                }
            }
        }

        public ILogger GetLogger()
        {
            return _Logger;
        }
    }
}
