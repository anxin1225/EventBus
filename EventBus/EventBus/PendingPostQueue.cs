using System;
using System.Threading;

namespace EventBusX
{
    public class PendingPostQueue
    {
        private PendingPost _Head;
        private PendingPost _Tail;

        private ManualResetEvent ResetEvent { get; set; } = new ManualResetEvent(false);

        public PendingPostQueue()
        {
        }

        public void Wait()
        {
            ResetEvent.WaitOne();
        }

        public void NotifyAll()
        {
            // 释放一个脉冲
            ResetEvent.Set();
            ResetEvent.Reset();
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

            // 安卓逻辑，通知处在等待该对象的线程的方法
            NotifyAll();
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
