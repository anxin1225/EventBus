using System;
using System.Threading;

namespace EventBusX
{
    public class ThreadLock<T>
    {
        private AsyncLocal<T> _Data;

        public T Data
        {
            get
            {
                if (_Data == null)
                {
                    lock (this)
                    {
                        if (_Data == null)
                            Init();
                    }
                }

                return _Data.Value;
            }
        }

        public virtual void Init()
        {
            _Data = new AsyncLocal<T>();
            var type = typeof(T);
            try
            {
                _Data.Value = (T)type.Assembly.CreateInstance(type.FullName);
            }
            catch
            {
                Console.WriteLine($"Init Data({type.Name}) Error");
            }
        }
    }
}
