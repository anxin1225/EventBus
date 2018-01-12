using System;
namespace EventBusX
{
    public class DefaultMainThreadSupport : MainThreadSupport
    {
        private Looper _Looper;

        public DefaultMainThreadSupport(Looper looper)
        {
            _Looper = looper;
        }

        public Poster CreatePoster(EventBus eventBus)
        {
            return new HandlerPoster(eventBus, looper, 10);
        }

        public bool IsMainThread()
        {
            return _Looper == Looper.MyLooper();
        }
    }
}
