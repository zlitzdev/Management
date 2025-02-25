using System;

namespace Zlitz.General.Management
{
    public sealed class CachedValue<T> : IReadOnlyValueHolder<T>
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
            return cachedValue == null ? default : cachedValue.value;
        }

        public CachedValue(Func<T> func, Predicate<T> shouldReset = null)
        {
            m_func = func;
            m_shouldReset = shouldReset;
        }
    }
}
