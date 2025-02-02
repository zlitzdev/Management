using UnityEngine.UI;
using UnityEngine.Events;

namespace Zlitz.General.Management
{
    public static class MenuUtils
    {
        public static void BindButton(Button button, UnityAction onClicked)
        {
            if (onClicked != null && button != null)
            {
                button.onClick.AddListener(onClicked);
            }
        }
    }
}
