using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
using System.Text;

namespace EventBusX
{
    public class SubscriberMethodFinder
    {
        public static ConcurrentDictionary<Type, List<SubscriberMethod>> MethodCache
            = new ConcurrentDictionary<Type, List<SubscriberMethod>>();

        private const int POOL_SIZE = 4;
        private static FindState[] FindStatePool = new FindState[POOL_SIZE];

        private bool IgnoreGeneratedIndex { get; set; }

        public static void ClearCache()
        {
            MethodCache.Clear();
        }

        public SubscriberMethodFinder()
        {
        }

        public List<SubscriberMethod> FindSubscriberMethods(Type type)
        {
            if (MethodCache.TryGetValue(type, out List<SubscriberMethod> list))
                return list;

            if(IgnoreGeneratedIndex)
            {
                FindUsingReflection(type);
            }
        }

        private List<SubscriberMethod> FindUsingReflection(Type type)
        {
            FindState findState = PrepareFindState();
            findState.InitForSubscriber(subscriberClass);
            while (findState.clazz != null)
            {
                findUsingReflectionInSingleClass(findState);
                findState.moveToSuperclass();
            }
            return getMethodsAndRelease(findState);
        }

        private List<SubscriberMethod> findUsingReflection(Type subscriberClass)
        {
            FindState findState = PrepareFindState();
            findState.InitForSubscriber(subscriberClass);
            while (findState.Clazz != null)
            {
                FindUsingReflectionInSingleClass(findState);
                findState.moveToSuperclass();
            }
            return getMethodsAndRelease(findState);
        }

        private void findUsingReflectionInSingleClass(FindState findState)
        {
            SubscriberMethodDelegate[] methods;
            try
            {
                // This is faster than getMethods, especially when subscribers are fat classes like Activities
                methods = findState.Clazz.DeclaringType;
            }
            catch (Throwable th)
            {
                // Workaround for java.lang.NoClassDefFoundError, see https://github.com/greenrobot/EventBus/issues/149
                methods = findState.clazz.getMethods();
                findState.skipSuperClasses = true;
            }
            for (Method method : methods)
            {
                int modifiers = method.getModifiers();
                if ((modifiers & Modifier.PUBLIC) != 0 && (modifiers & MODIFIERS_IGNORE) == 0)
                {
                    Class <?>[] parameterTypes = method.getParameterTypes();
                    if (parameterTypes.length == 1)
                    {
                        Subscribe subscribeAnnotation = method.getAnnotation(Subscribe.class);
                    if (subscribeAnnotation != null) {
                        Class<?> eventType = parameterTypes[0];
                        if (findState.checkAdd(method, eventType)) {
                            ThreadMode threadMode = subscribeAnnotation.threadMode();
        findState.subscriberMethods.add(new SubscriberMethod(method, eventType, threadMode,
                subscribeAnnotation.priority(), subscribeAnnotation.sticky()));
                        }
}
                } else if (strictMethodVerification && method.isAnnotationPresent(Subscribe.class)) {
                    String methodName = method.getDeclaringClass().getName() + "." + method.getName();
                    throw new EventBusException("@Subscribe method " + methodName +
                            "must have exactly 1 parameter but has " + parameterTypes.length);
                }
            } else if (strictMethodVerification && method.isAnnotationPresent(Subscribe.class)) {
                String methodName = method.getDeclaringClass().getName() + "." + method.getName();
                throw new EventBusException(methodName +
                        " is a illegal @Subscribe method: must be public, non-static, and non-abstract");
            }
        }
    }

        private FindState PrepareFindState()
        {
            lock(FindStatePool)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    FindState state = FindStatePool[i];
                    if (state != null)
                    {
                        FindStatePool[i] = null;
                        return state;
                    }
                }
            }
            return new FindState();
        }
    }

    class FindState
    {
        List<SubscriberMethod> _SubscriberMethods = new List<SubscriberMethod>();
        Dictionary<Type, object> _AnyMethodByEventType = new Dictionary<Type, object>();
        Dictionary<string, Type> _SubscriberClassByMethodKey = new Dictionary<string, Type>();
        StringBuilder _MethodKeyBuilder = new StringBuilder();

        public Type SubscriberClass;
        public Type Clazz;
        public bool SkipSuperClasses;
        public SubscriberInfo SubscriberInfo;

        public void InitForSubscriber(Type type)
        {
            SubscriberClass = Clazz = type;
            SkipSuperClasses = false;
            SubscriberInfo = null;
        }

        public void Recycle()
        {
            _SubscriberMethods.Clear();
            _AnyMethodByEventType.Clear();
            _SubscriberClassByMethodKey.Clear();
            _MethodKeyBuilder.Clear();
            SubscriberClass = null;
            Clazz = null;
            SkipSuperClasses = false;
            SubscriberInfo = null;
        }

        public bool CheckAdd(SubscriberMethodDelegate method, Type type)
        {
            lock (this)
            {
                object existing = _AnyMethodByEventType.AddNewAndReturnOld(type, method);
                if (existing == null)
                {
                    return true;
                }
                else
                {
                    if (existing is SubscriberMethodDelegate)
                    {
                        if (!CheckAddWithMethodSignature((SubscriberMethodDelegate)existing, type))
                        {
                            // Paranoia check
                            throw new Exception("Illegal State Exception");
                        }

                        _AnyMethodByEventType[type] = this;
                    }

                    return CheckAddWithMethodSignature(method, type);
                }
            }
        }

        private bool CheckAddWithMethodSignature(SubscriberMethodDelegate method, Type type)
        {
            _MethodKeyBuilder.Clear();
            _MethodKeyBuilder.Append(method.Method.Name);
            _MethodKeyBuilder.Append('>').Append(type.Name);

            string methodKey = _MethodKeyBuilder.ToString();
            Type method_type = method.Method.DeclaringType;
            Type method_type_old = _SubscriberClassByMethodKey.AddNewAndReturnOld(methodKey, method_type);

            if (method_type_old == null || method_type_old.IsAssignableFrom(method_type))
            {
                return true;
            }
            else
            {
                _SubscriberClassByMethodKey[methodKey] = method_type;
                return false;
            }
        }
    }
}
