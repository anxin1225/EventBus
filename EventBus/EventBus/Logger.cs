using System;
namespace EventBusX
{
    public interface ILogger
    {
        void log(Level level, string msg);
    }

    public class ConsoleLogger : ILogger
    {
        public void log(Level level, string msg)
        {
            Console.WriteLine($"[{level} {msg}]");
        }
    }
}
