using System;
namespace EventBusX
{
    public interface ILogger
    {
        void Log(string msg);

        void Log(Level level, string msg);

        void Log(Level level, string msg, Exception ex);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(Level level, string msg)
        {
            Console.WriteLine($"[{level} {msg}]");
        }

        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Log(Level level, string msg, Exception ex)
        {
            Console.WriteLine($"[{level}] {msg} {ex.Message}");
        }
    }
}
