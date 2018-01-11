using System;
namespace EventBusX
{
    public enum ThreadMode
    {
        POSTING,

        MAIN,

        MAIN_ORDERED,

        BACKGROUND,

        ASYNC,
    }
}
