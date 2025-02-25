using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    [Serializable]
    public class ObservableVariable<T> : IValueHolder<T>, IObservableValue<T>
    {
        [SerializeField]
        private T m_value;

        private UnityAction<T> m_onValueChanged;

        public T value 
        {
            get => m_value; 
            set
            {
                if (Comparer<T>.Default.Compare(m_value, value) != 0)
                {
                    m_value = value;
                    Notify();
                }
            }
        }

        public event UnityAction<T> onValueChanged
        {
            add    => m_onValueChanged += value;
            remove => m_onValueChanged -= value;
        }

        protected void Notify()
        {
            m_onValueChanged?.Invoke(m_value);
        }
    }
}
