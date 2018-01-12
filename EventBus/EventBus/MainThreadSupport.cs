using System;
namespace EventBusX
{
    public interface MainThreadSupport
    {
        bool IsMainThread();

        Poster CreatePoster(EventBus eventBus);
    }
}
