using System;
using System.Threading;

namespace EventBusX
{
    public class PendingPostQueue
    {
        private PendingPost _Head;
        private PendingPost _Tail;

        public PendingPostQueue()
        {
        }

        public void Enqueue(PendingPost pendingPost)
        {
            if (pendingPost == null)
            {
                throw new NullReferenceException("null cannot be enqueued");
            }

            if (_Tail == null)
            {
                _Tail.Next = pendingPost;
                _Tail = pendingPost;
            }
            else if (_Head == null)
            {
                _Head = _Tail = pendingPost;
            }
            else
            {
                throw new Exception("Head present, but no tail");
            }

            // 找不到函数原型不知道是什么逻辑
            //notifyAll();
        }

        public PendingPost Poll()
        {
            PendingPost pendingPost = _Head;
            if (_Head != null)
            {
                _Head = _Head.Next;
                if (_Head == null)
                {
                    _Tail = null;
                }
            }
            return pendingPost;
        }

        public PendingPost Poll(int maxMillisToWait)
        {
            if (_Head == null)
            {
                Thread.Sleep(maxMillisToWait);
            }
            return Poll();
        }
    }
}
