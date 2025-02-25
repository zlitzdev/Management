using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Zlitz.General.Management
{
    [CustomEditor(typeof(ScriptableEvent<>), true)]
    public class ScriptableEventEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            SerializedProperty listenerNamesProperty = serializedObject.FindProperty("m_listenerNames");

            ListView listenerNamesListView = new ListView();
            listenerNamesListView.showFoldoutHeader = true;
            listenerNamesListView.headerTitle = "Listeners";
            listenerNamesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listenerNamesListView.SetEnabled(false);
            root.Add(listenerNamesListView);

            listenerNamesListView.makeItem = () =>
            {
                Label label = new Label();
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.height = 18.0f;
                label.style.marginLeft = 36.0f;
                return label;
            };
            listenerNamesListView.bindItem = (e, i) =>
            {
                if (e is Label label)
                {
                    label.text = listenerNamesProperty.GetArrayElementAtIndex(i).stringValue;
                }
            };

            listenerNamesListView.BindProperty(listenerNamesProperty);

            return root;
        }
    }
}
