using System;
using System.Collections.Generic;

namespace EventBusX
{
    /// <summary>
    /// 回收池
    /// </summary>
    public class PendingPost
    {
        private static List<PendingPost> _PendingPostPool = new List<PendingPost>();

        public object Event { get; set; }
        public Subscription Subscription { get; set; }
        public PendingPost Next { get; internal set; }

        public PendingPost(Object event_obj, Subscription subscription)
        {
            Event = event_obj;
            Subscription = subscription;
        }

        public static PendingPost ObtainPendingPost(Subscription subscription, Object event_obj)
        {
            lock (_PendingPostPool)
            {
                var size = _PendingPostPool.Count;
                if (size > 0)
                {
                    PendingPost pending_post = _PendingPostPool[0];
                    _PendingPostPool.RemoveAt(0);

                    pending_post.Event = event_obj;
                    pending_post.Subscription = subscription;
                    pending_post.Next = null;
                    return pending_post;
                }
            }
            return new PendingPost(event_obj, subscription);
        }

        public static void ReleasePendingPost(PendingPost pending_post)
        {
            pending_post.Event = null;
            pending_post.Subscription = null;
            pending_post.Next = null;
            lock (_PendingPostPool)
            {
                if (_PendingPostPool.Count < 10000)
                {
                    _PendingPostPool.Add(pending_post);
                }
            }
        }
    }
}
