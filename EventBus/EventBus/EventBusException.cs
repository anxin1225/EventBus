using System;
namespace EventBusX
{
    public class EventBusException : Exception
    {
        public const long SERIAL_VERSION_UID = -2912559384646531479L;

        public EventBusException(string message)
            : base(message)
        {
        }

        public EventBusException(Exception ex)
            :base(null, ex)
        {
        }

        public EventBusException(string message, Exception ex)
            :base(message, ex)
        {
            
        }
    }
}
