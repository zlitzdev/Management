using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    [Serializable]
    public class ObservableList<T> : IList<T>, IReadOnlyValueHolder<IList<T>>, IObservableValue<IList<T>>
    {
        [SerializeField]
        private List<T> m_value;

        public IList<T> value => this;

        private UnityAction<int, T> m_onElementAdded;
        private UnityAction<int, T> m_onElementRemoved;
        private UnityAction<int, T> m_onElementChanged;

        private UnityAction<IList<T>> m_onValueChanged;

        public event UnityAction<int, T> onElementAdded
        {
            add => m_onElementAdded += value;
            remove => m_onElementAdded -= value;
        }

        public event UnityAction<int, T> onElementRemoved
        {
            add => m_onElementRemoved += value;
            remove => m_onElementRemoved -= value;
        }

        public event UnityAction<int, T> onElementChanged
        {
            add => m_onElementChanged += value;
            remove => m_onElementChanged -= value;
        }

        public event UnityAction<IList<T>> onValueChanged
        {
            add => m_onValueChanged += value;
            remove => m_onValueChanged -= value;
        }

        protected void Notify()
        {
            m_onValueChanged?.Invoke(this);
        }

        public T this[int index]
        {
            get => m_value[index];
            set
            {
                if (Comparer<T>.Default.Compare(m_value[index], value) != 0)
                {
                    T oldValue = m_value[index];
                    m_value[index] = value;
                    m_onElementChanged?.Invoke(index, oldValue);
                    Notify();
                }
            }
        }

        public int Count => m_value.Count;

        public bool IsReadOnly => ((ICollection<T>)m_value).IsReadOnly;

        public void Add(T item)
        {
            int index = m_value.Count;
            m_value.Add(item);
            m_onElementAdded?.Invoke(index, item);
            Notify();
        }

        public void Clear()
        {
            T[] oldValue = m_value.ToArray();
            m_value.Clear();
            for (int index = 0; index < oldValue.Length; index++)
            {
                m_onElementRemoved?.Invoke(index, oldValue[index]);
                Notify();
            }
        }

        public bool Contains(T item)
        {
            return m_value.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_value.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_value.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return m_value.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_value.Insert(index, item);
            m_onElementAdded?.Invoke(index, item);
            Notify();
        }

        public bool Remove(T item)
        {
            int index = m_value.IndexOf(item);
            if (index < 0 || index >= m_value.Count)
            {
                return false;
            }

            m_value.RemoveAt(index);
            m_onElementRemoved?.Invoke(index, item);
            Notify();
            return true;
        }

        public void RemoveAt(int index)
        {
            T item = m_value[index];
            m_value.RemoveAt(index);
            m_onElementRemoved.Invoke(index, item);
            Notify();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_value.GetEnumerator();
        }
    }
}
