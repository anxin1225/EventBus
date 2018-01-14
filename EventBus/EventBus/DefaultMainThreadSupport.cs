using System;
namespace EventBusX
{
    public class DefaultMainThreadSupport : IMainThreadSupport
    {
        private Looper _Looper;

        public DefaultMainThreadSupport(Looper looper)
        {
            _Looper = looper;
        }

        public IPoster CreatePoster(EventBus eventBus)
        {
            return new HandlerPoster(eventBus, _Looper, 10);
        }

        public bool IsMainThread()
        {
            return _Looper == Looper.MyLooper();
        }
    }
}
