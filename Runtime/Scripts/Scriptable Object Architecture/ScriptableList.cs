using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    public abstract class ScriptableList<T> : BaseScriptableObject, IList<T>, IReadOnlyValueHolder<IList<T>>, IObservableValue<IList<T>>
    {
        [SerializeField]
        private T[] m_initialValue;

        [SerializeField]
        private List<T> m_currentValue;

        public IList<T> value => this;

        private UnityAction<int, T> m_onElementAdded;
        private UnityAction<int, T> m_onElementRemoved;
        private UnityAction<int, T> m_onElementChanged;

        private UnityAction<IList<T>> m_onValueChanged;

        public event UnityAction<int, T> onElementAdded
        {
            add    => m_onElementAdded += value;
            remove => m_onElementAdded -= value;
        }

        public event UnityAction<int, T> onElementRemoved
        {
            add    => m_onElementRemoved += value;
            remove => m_onElementRemoved -= value;
        }

        public event UnityAction<int, T> onElementChanged
        {
            add    => m_onElementChanged += value;
            remove => m_onElementChanged -= value;
        }

        public event UnityAction<IList<T>> onValueChanged
        {
            add    => m_onValueChanged += value;
            remove => m_onValueChanged -= value;
        }

        protected override void OnReset()
        {
            m_onElementAdded   = null;
            m_onElementRemoved = null;
            m_onElementChanged = null;
            m_currentValue = m_initialValue == null ? new List<T>() : new List<T>(m_initialValue);
        }

        protected void Notify()
        {
            m_onValueChanged?.Invoke(this);
        }

        public T this[int index] 
        { 
            get => m_currentValue[index]; 
            set
            {
                if (Comparer<T>.Default.Compare(m_currentValue[index], value) != 0)
                {
                    T oldValue = m_currentValue[index];
                    m_currentValue[index] = value;
                    m_onElementChanged?.Invoke(index, oldValue);
                   Notify();
                }
            }
        }

        public int Count => m_currentValue.Count;

        public bool IsReadOnly => ((ICollection<T>)m_currentValue).IsReadOnly;

        public void Add(T item)
        {
            int index = m_currentValue.Count;
            m_currentValue.Add(item);
            m_onElementAdded?.Invoke(index, item);
           Notify();
        }

        public void Clear()
        {
            T[] oldValue = m_currentValue.ToArray();
            m_currentValue.Clear();
            for (int index = 0; index < oldValue.Length; index++)
            {
                m_onElementRemoved?.Invoke(index, oldValue[index]);
               Notify();
            }
        }

        public bool Contains(T item)
        {
            return m_currentValue.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_currentValue.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_currentValue.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return m_currentValue.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_currentValue.Insert(index, item);
            m_onElementAdded?.Invoke(index, item);
           Notify();
        }

        public bool Remove(T item)
        {
            int index = m_currentValue.IndexOf(item);
            if (index < 0 || index >= m_currentValue.Count)
            {
                return false;
            }

            m_currentValue.RemoveAt(index);
            m_onElementRemoved?.Invoke(index, item);
           Notify();
            return true;
        }

        public void RemoveAt(int index)
        {
            T item = m_currentValue[index];
            m_currentValue.RemoveAt(index);
            m_onElementRemoved.Invoke(index, item);
           Notify();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_currentValue.GetEnumerator();
        }
    }
}
