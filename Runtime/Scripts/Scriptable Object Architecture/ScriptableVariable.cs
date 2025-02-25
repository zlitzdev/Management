using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    public abstract class ScriptableVariable<T> : BaseScriptableObject, IValueHolder<T>, IObservableValue<T>
    {
        [SerializeField]
        private T m_initialValue;

        [SerializeField]
        private T m_currentValue;

        private UnityAction<T> m_onValueChanged;

        public T value
        {
            get => m_currentValue;
            set
            {
                if (Comparer<T>.Default.Compare(m_currentValue, value) != 0)
                {
                    m_currentValue = value;
                    Notify();
                }
            }
        }

        public event UnityAction<T> onValueChanged
        {
            add    => m_onValueChanged += value;
            remove => m_onValueChanged -= value;
        }

        protected override void OnReset()
        {
            m_onValueChanged = null;
            m_currentValue = m_initialValue;
        }

        protected void Notify()
        {
            m_onValueChanged?.Invoke(m_currentValue);
        }
    }
}
