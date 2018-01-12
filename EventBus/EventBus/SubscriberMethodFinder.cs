﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
using System.Text;
using EventBusX;

namespace EventBusX
{
    public class SubscriberMethodFinder
    {
        public static ConcurrentDictionary<Type, List<SubscriberMethod>> MethodCache
            = new ConcurrentDictionary<Type, List<SubscriberMethod>>();

        private const int POOL_SIZE = 4;
        private static FindState[] FindStatePool = new FindState[POOL_SIZE];
        private List<SubscriberInfoIndex> SubscriberInfoIndexes;

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

            if (IgnoreGeneratedIndex)
            {
                list = FindUsingReflection(type);
            }
            else
            {
                list = FindUsingInfo(type);
            }

            if(list == null || list.Count == 0)
            {
                throw new EventBusException("Subscriber " + type
                    + " and its super classes have no public methods with the @Subscribe annotation");
            }
            else
            {
                MethodCache[type] = list;
                return list;
            }
        }

        private List<SubscriberMethod> FindUsingInfo(Type type)
        {
            FindState findState = PrepareFindState();
            findState.InitForSubscriber(type);
            while (findState.Clazz != null)
            {
                findState.SubscriberInfo = GetSubscriberInfo(findState);
                if (findState.SubscriberInfo != null)
                {
                    SubscriberMethod[] array = findState.SubscriberInfo.GetSubscriberMethods();
                    foreach (SubscriberMethod subscriberMethod in array)
                    //for (SubscriberMethod subscriberMethod : array)
                    {
                        if (findState.CheckAdd(subscriberMethod.Method, subscriberMethod.EventType))
                        {
                            findState.SubscriberMethods.Add(subscriberMethod);
                        }
                    }
                }
                else
                {
                    FindUsingReflectionInSingleClass(findState);
                }
                findState.MoveToSuperclass();
            }
            return GetMethodsAndRelease(findState);
        }

        private List<SubscriberMethod> FindUsingReflection(Type type)
        {
            FindState findState = PrepareFindState();
            findState.InitForSubscriber(type);
            while (findState.Clazz != null)
            {
                FindUsingReflectionInSingleClass(findState);
                findState.MoveToSuperclass();
            }
            return GetMethodsAndRelease(findState);
        }

        private SubscriberInfo GetSubscriberInfo(FindState findState)
        {
            if (findState.SubscriberInfo != null && findState.SubscriberInfo.GetSuperSubscriberInfo() != null)
            {
                SubscriberInfo superclassInfo = findState.SubscriberInfo.GetSuperSubscriberInfo();
                if (findState.Clazz == superclassInfo.GetSubscriberClass())
                {
                    return superclassInfo;
                }
            }
            if (SubscriberInfoIndexes != null)
            {
                foreach(var item in SubscriberInfoIndexes)
                //for (SubscriberInfoIndex index : subscriberInfoIndexes)
                {
                    SubscriberInfo info = item.GetSubscriberInfo(findState.Clazz);
                    if (info != null)
                    {
                        return info;
                    }
                }
            }
            return null;
        }

        private List<SubscriberMethod> GetMethodsAndRelease(FindState findState)
        {
            List<SubscriberMethod> subscriberMethods = new List<SubscriberMethod>();
            findState.Recycle();
            lock(FindStatePool) {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    if (FindStatePool[i] == null)
                    {
                        FindStatePool[i] = findState;
                        break;
                    }
                }
            }
            return subscriberMethods;
        }

        private void FindUsingReflectionInSingleClass(FindState findState)
        {
            //            SubscriberMethodDelegate[] methods;
            //            try
            //            {
            //                // This is faster than getMethods, especially when subscribers are fat classes like Activities
            //                methods = findState.Clazz.DeclaringType;
            //            }
            //            catch (Throwable th)
            //            {
            //                // Workaround for java.lang.NoClassDefFoundError, see https://github.com/greenrobot/EventBus/issues/149
            //                methods = findState.clazz.getMethods();
            //                findState.skipSuperClasses = true;
            //            }
            //            for (Method method : methods)
            //            {
            //                int modifiers = method.getModifiers();
            //                if ((modifiers & Modifier.PUBLIC) != 0 && (modifiers & MODIFIERS_IGNORE) == 0)
            //                {
            //                    Class <?>[] parameterTypes = method.getParameterTypes();
            //                    if (parameterTypes.length == 1)
            //                    {
            //                        Subscribe subscribeAnnotation = method.getAnnotation(Subscribe.class);
            //                    if (subscribeAnnotation != null) {
            //                        Class<?> eventType = parameterTypes[0];
            //                        if (findState.checkAdd(method, eventType)) {
            //                            ThreadMode threadMode = subscribeAnnotation.threadMode();
            //        findState.subscriberMethods.add(new SubscriberMethod(method, eventType, threadMode,
            //                subscribeAnnotation.priority(), subscribeAnnotation.sticky()));
            //                        }
            //}
            //    } else if (strictMethodVerification && method.isAnnotationPresent(Subscribe.class)) {
            //        String methodName = method.getDeclaringClass().getName() + "." + method.getName();
            //        throw new EventBusException("@Subscribe method " + methodName +
            //                "must have exactly 1 parameter but has " + parameterTypes.length);
            //    }
            //} else if (strictMethodVerification && method.isAnnotationPresent(Subscribe.class)) {
            //    String methodName = method.getDeclaringClass().getName() + "." + method.getName();
            //    throw new EventBusException(methodName +
            //            " is a illegal @Subscribe method: must be public, non-static, and non-abstract");
            //}
            //}]
        }

        private FindState PrepareFindState()
        {
            lock (FindStatePool)
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
        public List<SubscriberMethod> SubscriberMethods = new List<SubscriberMethod>();
        public Dictionary<Type, object> AnyMethodByEventType = new Dictionary<Type, object>();
        public Dictionary<string, Type> SubscriberClassByMethodKey = new Dictionary<string, Type>();
        public StringBuilder MethodKeyBuilder = new StringBuilder();

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
            SubscriberMethods.Clear();
            AnyMethodByEventType.Clear();
            SubscriberClassByMethodKey.Clear();
            MethodKeyBuilder.Clear();
            SubscriberClass = null;
            Clazz = null;
            SkipSuperClasses = false;
            SubscriberInfo = null;
        }

        public bool CheckAdd(SubscriberMethodDelegate method, Type type)
        {
            lock (this)
            {
                object existing = AnyMethodByEventType.AddNewAndReturnOld(type, method);
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

                        AnyMethodByEventType[type] = this;
                    }

                    return CheckAddWithMethodSignature(method, type);
                }
            }
        }

        private bool CheckAddWithMethodSignature(SubscriberMethodDelegate method, Type type)
        {
            MethodKeyBuilder.Clear();
            MethodKeyBuilder.Append(method.Method.Name);
            MethodKeyBuilder.Append('>').Append(type.Name);

            string methodKey = MethodKeyBuilder.ToString();
            Type method_type = method.Method.DeclaringType;
            Type method_type_old = SubscriberClassByMethodKey.AddNewAndReturnOld(methodKey, method_type);

            if (method_type_old == null || method_type_old.IsAssignableFrom(method_type))
            {
                return true;
            }
            else
            {
                SubscriberClassByMethodKey[methodKey] = method_type;
                return false;
            }
        }

        /// <summary>
        /// 不知到有什么用
        /// </summary>
        public void MoveToSuperclass()
        {
            if (SkipSuperClasses)
            {
                Clazz = null;
            }
            else
            {
                Clazz = Clazz.BaseType;
                //String clazzName = Clazz.getName();
                /** Skip system classes, this just degrades performance. */
                //if (clazzName.startsWith("java.") || clazzName.startsWith("javax.") || clazzName.startsWith("android."))
                //{
                //    clazz = null;
                //}
            }
        }
    }
}
