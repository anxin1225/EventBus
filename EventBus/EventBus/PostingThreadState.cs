using System;
using System.Collections.Generic;

namespace EventBusX
{
    public class PostingThreadState
    {
        public List<Object> EventQueue { get; set; } = new List<object>();

        public bool IsPosting { get; set; } //是否正在执行postSingleEvent()方法

        public bool IsMainThread { get; set; }

        public Subscription Subscription { get; set; }

        public Object Event { get; set; }

        public bool Canceled { get; set; }
    }
}
