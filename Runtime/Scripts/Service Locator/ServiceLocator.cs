using System;
using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    [AddComponentMenu("")]
    internal class ServiceLocator : MonoBehaviour, IServiceLocator
    {
        private readonly Dictionary<Type, object> m_services = new Dictionary<Type, object>();

        private static ServiceLocator s_global;

        internal static ServiceLocator global;

        public bool Register<T>(T service) where T : class
        {
            return m_services.TryAdd(typeof(T), service);
        }

        public bool Unregister<T>() where T : class
        {
            return m_services.Remove(typeof(T));
        }

        public T Get<T>() where T : class
        {
            if (!m_services.TryGetValue(typeof(T), out object service) || service is not T result)
            {
                return null;
            }
            return result;
        }

        internal void Clear()
        {
            m_services.Clear();
        }

        internal static ServiceLocator Global()
        {
            if (s_global == null)
            {
                GameObject globalObject = new GameObject("Global Service Locator");
                globalObject.hideFlags =  HideFlags.DontSave | HideFlags.HideInHierarchy;
                DontDestroyOnLoad(globalObject);

                s_global = globalObject.AddComponent<ServiceLocator>();
            }
            return s_global;
        }

        internal static ServiceLocator Of(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out ServiceLocator serviceLocator))
            {
                serviceLocator = gameObject.AddComponent<ServiceLocator>();
                serviceLocator.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;
            }
            return serviceLocator;
        }
    }
}
