using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.General.Management
{
    [CustomEditor(typeof(MenuStack))]
    public class MenuStackEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            PropertyField idField = new PropertyField(serializedObject.FindProperty("m_id"));
            idField.SetEnabled(!Application.isPlaying);
            root.Add(idField);

            return root;
        }
    }
}
