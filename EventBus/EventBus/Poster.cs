using System;
namespace EventBusX
{
    public interface IPoster
    {
        /// <summary>
        /// Enqueue an event to be posted for a particular subscription.
        /// </summary>
        /// <returns>The enqueue.</returns>
        /// <param name="subscription">Subscription.</param>
        /// <param name="event_obj">Event object.</param>
        void Enqueue(Subscription subscription, Object event_obj);
    }
}
