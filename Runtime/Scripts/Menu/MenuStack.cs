using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("Zlitz/General/Management/Menu Stack")]
    public sealed class MenuStack : MonoBehaviour
    {
        [SerializeField]
        private string m_id;

        public event MenuChangedCallback onMenuChanged;

        private readonly Stack<Menu> m_menus = new Stack<Menu>();

        private static Dictionary<string, MenuStack> s_instances = new Dictionary<string, MenuStack>();

        public Menu current
        {
            get
            {
                if (m_menus.TryPeek(out Menu menu))
                {
                    return menu;
                }
                return null;
            }
        }

        public void Push(Menu menuPrefab, object menuParam)
        {
            if (m_menus.TryPeek(out Menu currentMenu))
            {
                currentMenu.gameObject.SetActive(false);
            }

            Menu menu = Instantiate(menuPrefab, transform);
            menu.InitializeInternal(this, m_menus.Count, menuParam);

            m_menus.Push(menu);
            onMenuChanged?.Invoke(menu);
        }

        public void Pop()
        {
            if (m_menus.TryPop(out Menu menu))
            {
                menu.OnPoppedInternal();
                Destroy(menu.gameObject);

                if (m_menus.TryPeek(out Menu currentMenu))
                {
                    currentMenu.gameObject.SetActive(true);
                    onMenuChanged?.Invoke(currentMenu);
                }
                else
                {
                    onMenuChanged?.Invoke(null);
                }
            }
        }

        public void PopTo(int menuId)
        {
            Menu previousMenu = current;

            while (m_menus.TryPop(out Menu menu))
            {
                bool shouldStop = menu.menuId == menuId;

                menu.OnPoppedInternal();
                Destroy(menu.gameObject);

                if (shouldStop)
                {
                    break;
                }
            }

            if (m_menus.TryPeek(out Menu currentMenu))
            {
                currentMenu.gameObject.SetActive(true);
                if (previousMenu != currentMenu)
                {
                    onMenuChanged?.Invoke(currentMenu);
                }
            }
            else if (previousMenu != null)
            {
                onMenuChanged?.Invoke(null);
            }
        }

        private void Awake()
        {
            s_instances.TryAdd(m_id, this);
        }

        private void OnDestroy()
        {
            PopTo(0);
            if (s_instances.TryGetValue(m_id, out MenuStack menuStack) && menuStack == this)
            {
                s_instances.Remove(m_id);
            }
        }

        public static MenuStack Get(string id)
        {
            if (!s_instances.TryGetValue(id, out MenuStack menuStack))
            {
                menuStack = null;
            }
            return menuStack;
        }

        public static CachedValue<MenuStack> GetCached(string id)
        {
            return new CachedValue<MenuStack>(() => Get(id));
        }

        public delegate void MenuChangedCallback(Menu menu);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            s_instances.Clear();
        }
    }
}
