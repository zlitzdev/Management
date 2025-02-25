using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.General.Management
{
    [CustomPropertyDrawer(typeof(ScriptableEvent<>), true)]
    public class ScriptableEventDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();

            object val = SerializedPropertyHelper.GetPropertyValue(property, out Type propertyType);

            ObjectField eventField = new ObjectField(property.displayName);
            eventField.objectType = propertyType;
            eventField.AddToClassList("unity-base-field__aligned");
            eventField.BindProperty(property);
            root.Add(eventField);

            EventListenerField eventListenersField = new EventListenerField(property);
            eventListenersField.eventObject = property.objectReferenceValue;
            root.Add(eventListenersField);

            eventField.RegisterValueChangedCallback(e =>
            {
                eventListenersField.eventObject = property.objectReferenceValue;
            });

            return root;
        }
    
        private class EventListenerField : VisualElement
        {
            private UnityEngine.Object m_eventObject;

            private SerializedObject   m_serializedEvent;
            private SerializedProperty m_listenerNamesProperty;

            public UnityEngine.Object eventObject
            {
                get => m_eventObject;
                set
                {
                    if (m_eventObject != value)
                    {
                        m_eventObject = value;
                        m_serializedEvent = m_eventObject == null ? null : new SerializedObject(m_eventObject);
                        m_listenerNamesProperty = m_serializedEvent?.FindProperty("m_listenerNames");

                        m_foldout.Clear();
                        m_foldout.style.display = DisplayStyle.None;
                        if (m_listenerNamesProperty != null)
                        {
                            ListView listenerNamesListView = new ListView();
                            listenerNamesListView.showFoldoutHeader = true;
                            listenerNamesListView.headerTitle = "Listeners";
                            listenerNamesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                            listenerNamesListView.SetEnabled(false);

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
                                    label.text = m_listenerNamesProperty.GetArrayElementAtIndex(i).stringValue;
                                }
                            };

                            listenerNamesListView.BindProperty(m_listenerNamesProperty);

                            m_foldout.Add(listenerNamesListView);
                            m_foldout.style.display = DisplayStyle.Flex;
                        }
                    }
                }
            }

            private SerializedProperty m_property;

            private Foldout m_foldout;

            public EventListenerField(SerializedProperty property)
            {
                style.marginTop = -18.0f;
                style.minHeight = 18.0f;
                pickingMode = PickingMode.Ignore;

                m_property = property;

                m_foldout = new Foldout();
                m_foldout.pickingMode = PickingMode.Ignore;
                m_foldout.value = m_property.isExpanded;
                m_foldout.RegisterValueChangedCallback(e =>
                {
                    m_property.isExpanded = e.newValue;
                });
                Add(m_foldout);

                Toggle foldoutToggle = m_foldout.Q<Toggle>();
                foldoutToggle.style.width = 18.0f;
                foldoutToggle.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

                m_foldout.style.display = DisplayStyle.None;
            }
        }
    }
}
