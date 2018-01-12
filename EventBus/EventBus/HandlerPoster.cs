using System;
namespace EventBusX
{
    public class HandlerPoster : Poster, IHandler
    {
        private PendingPostQueue _Queue;
        private bool _HandlerActive;
        private EventBus _EventBus;
        private Looper _Looper;
        private int _MaxMillisInsideHandleMessage;

        public HandlerPoster(EventBus eventBus, Looper looper, int maxMillisInsideHandleMessage)
        {
            _Looper = looper;
            _EventBus = eventBus;
            _MaxMillisInsideHandleMessage = maxMillisInsideHandleMessage;
            _Queue = new PendingPostQueue();
        }

        public void Enqueue(Subscription subscription, object event_obj)
        {
            PendingPost pendingPost = PendingPost.ObtainPendingPost(subscription, event_obj);
            lock (this)
            {
                _Queue.Enqueue(pendingPost);

                if(!_HandlerActive)
                {
                    _HandlerActive = true;

                    //// 这两个函数不知道是干啥的
                    //if (!SendMessage(ObtainMessage()))
                    //{
                    //    throw new EventBusException("Could not send handler message");
                    //}
                }
            }
        }

        public void HandleMessage(Message msg)
        {
            bool rescheduled = false;
            try
            {
                long start = DateTime.Now.Ticks / 10000;
                while (true)
                {
                    PendingPost pending_post = _Queue.Poll();
                    if (pending_post == null)
                    {
                        lock (this)
                        {
                            pending_post = _Queue.Poll();
                            if (pending_post == null)
                            {
                                _HandlerActive = false;
                                return;
                            }
                        }
                    }

                    _EventBus.
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
