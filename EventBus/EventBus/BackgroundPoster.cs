using System;
using System.Threading;

namespace EventBusX
{
    public class BackgroundPoster : IRunnable, IPoster
    {
        private PendingPostQueue _Queue;
        private EventBus _EventBus;

        private bool _ExecutorRunning;

        public BackgroundPoster(EventBus bus)
        {
            _EventBus = bus;
            _Queue = new PendingPostQueue();
        }

        public void Enqueue(Subscription subscription, object event_obj)
        {
            PendingPost pendingPost = PendingPost.ObtainPendingPost(subscription, event_obj);
            lock (this)
            {
                _Queue.Enqueue(pendingPost);
                if (!_ExecutorRunning)
                {
                    _ExecutorRunning = true;
                    ThreadPool.QueueUserWorkItem(n => { Run(); });
                }
            }
        }

        public void Run()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        PendingPost pending_post = _Queue.Poll();
                        if (pending_post == null)
                        {
                            lock (this)
                            {
                                pending_post = _Queue.Poll();
                                _ExecutorRunning = false;
                                return;
                            }
                        }
                        _EventBus.InvokeSubscriber(pending_post);
                    }
                }
                catch (Exception ex)
                {
                    _EventBus.GetLogger().Log(Level.WARNING, Thread.CurrentThread.Name + " was interruppted", ex);
                }
            }
            finally
            {
                _ExecutorRunning = false;
            }
        }
    }
}
