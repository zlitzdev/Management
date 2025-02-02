using System;

using UnityEngine;

namespace Zlitz.General.Management
{
    public sealed class CachedValue<T>
    {
        private T m_value;

        private bool m_calculated;

        private Func<T> m_func;
        private Predicate<T> m_shouldReset;

        public T value
        {
            get
            {
                if (!m_calculated || (m_shouldReset?.Invoke(m_value) ?? false))
                {
                    m_calculated = true;

                    Debug.Log("Recalculating");
                    m_value = m_func == null ? default(T) : m_func.Invoke();
                }
                return m_value;
            }
        }

        public void Reset()
        {
            m_calculated = false;
        }

        public static implicit operator T(CachedValue<T> cachedValue)
        {
            return cachedValue.value;
        }

        public CachedValue(Func<T> func, Predicate<T> shouldReset = null)
        {
            m_func = func;
            m_shouldReset = shouldReset;
        }
    }
}
