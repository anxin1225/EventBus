using System;
namespace EventBusX
{
    public class PendingPost
    {
        public object Event { get; set; }
        public Subscription Subscription { get; set; }
        public PendingPost Next { get; internal set; }

        public PendingPost()
        {
        }

        public static PendingPost ObtainPendingPost(Subscription subscription, Object event_obj)
        {
            throw new NotImplementedException();
        }

        public static void ReleasePendingPost(PendingPost pending_post)
        {
            throw new NotImplementedException();
        }
    }
}
