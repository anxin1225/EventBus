using System;
namespace EventBusX
{
    public interface IHandler
    {
        void HandleMessage(Message msg);
    }
}
