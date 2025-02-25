using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    public abstract class ScriptableEvent<T> : BaseScriptableObject
    {
        [SerializeField]
        private List<string> m_listenerNames = new List<string>();

        private readonly List<IListener> m_listeners = new List<IListener>();

        public static ScriptableEvent<T> operator+(ScriptableEvent<T> gameEvent, IListener listener)
        {
            if (listener != null)
            {
                gameEvent.m_listeners.Add(listener);
                gameEvent.m_listenerNames.Add(listener.debugName);
            }
            return gameEvent;
        }

        public static ScriptableEvent<T> operator-(ScriptableEvent<T> gameEvent, IListener listener)
        {
            for (int i = gameEvent.m_listeners.Count - 1; i >= 0; i--)
            {
                if (gameEvent.m_listeners[i] == listener)
                {
                    gameEvent.m_listeners.RemoveAt(i);
                    gameEvent.m_listenerNames.RemoveAt(i);
                    break;
                }
            }
            return gameEvent;
        }

        public static ScriptableEvent<T> operator+(ScriptableEvent<T> gameEvent, UnityAction<T> callback)
        {
            if (callback != null)
            {
                IListener newListener = new UnityActionListener(callback);
                gameEvent.m_listeners.Add(newListener);
                gameEvent.m_listenerNames.Add(newListener.debugName);
            }
            return gameEvent;
        }

        public static ScriptableEvent<T> operator-(ScriptableEvent<T> gameEvent, UnityAction<T> callback)
        {
            for (int i = gameEvent.m_listeners.Count - 1; i >= 0; i--)
            {
                UnityActionListener listener = gameEvent.m_listeners[i] as UnityActionListener;
                if (listener != null && listener.Compare(callback))
                {
                    gameEvent.m_listeners.RemoveAt(i);
                    gameEvent.m_listenerNames.RemoveAt(i);
                    break;
                }
            }
            return gameEvent;
        }

        public void Invoke(T eventData) 
        {
            for (int i = m_listeners.Count - 1; i >= 0; i--)
            {
                m_listeners[i]?.OnEvent(eventData);
            }
        }

        protected override void OnReset()
        {
            m_listeners.Clear();
            m_listenerNames.Clear();
        }

        public interface IListener
        {
            string debugName { get; }

            void OnEvent(T eventData);
        }

        private class UnityActionListener : IListener
        {
            private UnityAction<T> m_unityAction;

            public bool Compare(UnityAction<T> unityAction)
            {
                return m_unityAction == unityAction;
            }

            public UnityActionListener(UnityAction<T> unityAction)
            {
                m_unityAction = unityAction;
            }

            public string debugName => GetDebugName(m_unityAction);

            public void OnEvent(T eventData)
            {
                m_unityAction?.Invoke(eventData);
            }

            private static string GetDebugName(Delegate action)
            {
                if (action == null)
                {
                    return "Null";
                }

                Delegate[] methodList = action.GetInvocationList();
                string[] debugNames = new string[methodList.Length];

                for (int i = 0; i < methodList.Length; i++)
                {
                    MethodInfo method = methodList[i].Method;
                    object     target = methodList[i].Target;

                    string targetName = target != null ? target.GetType().Name : "Static";
                    debugNames[i] = $"{targetName}.{method.Name}";
                }

                return string.Join(" | ", debugNames);
            }
        }
    }
}
