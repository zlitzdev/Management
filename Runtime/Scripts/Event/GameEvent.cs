using System;

namespace Zlitz.General.Management
{
    public sealed class GameEvent<T>
    {
        private event Action<T> m_event;

        public void Subscribe(Action<T> callback)
        {
            m_event += callback;
        }

        public void Unsubscribe(Action<T> callback)
        {
            m_event -= callback;
        }

        public void Invoke(T eventData)
        {
            m_event?.Invoke(eventData);
        }
    }
}
