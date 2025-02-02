using System.Collections.Generic;

namespace Zlitz.General.Management
{
    public class Observable<T>
    {
        public event ValueChangeCallback onValueChanged;

        private T m_value;

        public T value
        {
            get => m_value;
            set
            {
                if (Comparer<T>.Default.Compare(m_value, value) != 0)
                {
                    m_value = value;
                    onValueChanged?.Invoke(m_value);
                }
            }
        }

        public void SetWithoutNotify(T value)
        {
            m_value = value;
        }

        public void Notify()
        {
            onValueChanged?.Invoke(m_value);
        }

        public Observable(T initialValue = default)
        {
            m_value = value;
        }

        public delegate void ValueChangeCallback(T newValue);
    }
}
