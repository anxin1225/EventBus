using System;
namespace EventBusX
{
    public class IllegalStateException : Exception
    {
        public IllegalStateException()
        {
        }

        public IllegalStateException(string message)
            :base(message)
        {
            
        }
    }
}
