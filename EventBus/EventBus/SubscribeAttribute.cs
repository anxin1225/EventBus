using System;
namespace EventBusX
{
    public class SubscribeAttribute : Attribute
    {
        /// <summary>
        /// 线程属性
        /// </summary>
        /// <value>The thread mode.</value>
        public ThreadMode ThreadMode { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; set; }

        /// <summary>
        /// 粘性
        /// </summary>
        /// <value><c>true</c> if sticky; otherwise, <c>false</c>.</value>
        public bool Sticky { get; set; }

        public SubscribeAttribute()
        {
        }
    }
}
