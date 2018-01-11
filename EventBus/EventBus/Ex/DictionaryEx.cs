using System;
using System.Collections.Generic;
namespace EventBusX
{
    public static class DictionaryEx
    {
        public static VT AddNewAndReturnOld<KT, VT>(this Dictionary<KT, VT> _dic, KT key, VT value)
        {
            var old_value = _dic.ContainsKey(key) ? _dic[key] : default(VT);
            _dic[key] = value;
            return old_value;
        }
    }
}
