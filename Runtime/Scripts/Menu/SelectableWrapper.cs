using System;

using UnityEngine;
using UnityEngine.UI;

namespace Zlitz.General.Management
{
    [Serializable]
    public sealed class SelectableWrapper<T> where T : Selectable
    {
        [SerializeField]
        private T m_selectable;

        [NonSerialized]
        private int m_disableCount = 0;

        public T selectable
        {
            get => m_selectable;
            set
            {
                m_selectable = value;
                Validate();
            }
        }

        public void EnableInteraction()
        {
            if (m_disableCount > 0)
            {
                m_disableCount--;
                Validate();
            }
        }

        public void DisableInteraction()
        {
            m_disableCount++;
            Validate();
        }

        public static implicit operator T(SelectableWrapper<T> wrapper)
        {
            return wrapper?.m_selectable;
        }

        private void Validate()
        {
            if (m_selectable != null)
            {
                m_selectable.interactable = m_disableCount <= 0;
            }
        }
    }
}
