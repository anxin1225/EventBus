using System;
using System.Threading;

namespace EventBusX
{
    public class AsyncPoster : IRunnable, IPoster
    {
        private PendingPostQueue _Queue;
        private EventBus _EventBus;

        public AsyncPoster(EventBus bus)
        {
            _EventBus = bus;
            _Queue = new PendingPostQueue();
        }

        public void Enqueue(Subscription subscription, object event_obj)
        {
            PendingPost pending_post = PendingPost.ObtainPendingPost(subscription, event_obj);
            _Queue.Enqueue(pending_post);
            ThreadPool.QueueUserWorkItem(n => { Run(); });
        }

        public void Run()
        {
            PendingPost pending_post = _Queue.Poll();
            if (pending_post == null)
            {
                throw new IllegalStateException("No pending post available");
            }
            _EventBus.InvokeSubscriber(pending_post);
        }
    }
}
