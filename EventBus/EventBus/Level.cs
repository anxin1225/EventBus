using System;
namespace EventBusX
{
    public enum Level : byte
    {
        INFO = 1 << 1,

        WARNING = 1 << 2,

        SEVERE = 1 << 3,

        OFF = 1 << 7,
    }
}
