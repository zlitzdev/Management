using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Zlitz.General.Management
{
    [CustomEditor(typeof(ScriptableList<>), true)]
    public class ScriptableListEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            PropertyField initialValueField = new PropertyField(serializedObject.FindProperty("m_initialValue"));
            root.Add(initialValueField);

            PropertyField currentValueField = new PropertyField(serializedObject.FindProperty("m_currentValue"));
            currentValueField.SetEnabled(false);
            root.Add(currentValueField);

            return root;
        }
    }
}
