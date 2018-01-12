using System;
namespace EventBusX
{
    /// <summary>
    /// Android 中的概念，消息循环，可能需要重新自己定义
    /// </summary>
    public class Looper
    {
        public Looper()
        {
        }

        internal static Looper MyLooper()
        {
            throw new NotImplementedException();
        }
    }
}
