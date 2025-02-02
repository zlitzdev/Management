using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Zlitz.General.Management
{
    [CustomPropertyDrawer(typeof(SelectableWrapper<>))]
    public class SelectableWrapperDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative("m_selectable"));
        }
    }
}
