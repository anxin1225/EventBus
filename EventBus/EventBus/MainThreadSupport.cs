using System;
namespace EventBusX
{
    public interface IMainThreadSupport
    {
        bool IsMainThread();

        IPoster CreatePoster(EventBus eventBus);
    }
}
