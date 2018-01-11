using System;
namespace EventBusX
{
    public class EventBusBuilder
    {
        private ILogger _Logger;

        public EventBusBuilder()
        {
        }

        public ILogger GetLogger()
        {
            return _Logger;
        }
    }
}
