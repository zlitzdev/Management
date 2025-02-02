using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class Menu : MonoBehaviour
    {
        private MenuStack m_menuStack;
        private int m_menuId;

        protected MenuStack menuStack => m_menuStack;

        public int menuId => m_menuId;
        
        protected virtual void Initialize(object menuParam)
        {
        }

        protected virtual void OnPopped()
        {
        }

        internal void InitializeInternal(MenuStack menuStack, int menuId, object menuParam)
        {
            m_menuStack = menuStack;
            m_menuId    = menuId;

            Initialize(menuParam);
        }

        internal void OnPoppedInternal()
        {
            OnPopped();
        }
    }
}
