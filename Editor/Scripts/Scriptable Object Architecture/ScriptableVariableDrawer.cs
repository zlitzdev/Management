using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.General.Management
{
    [CustomPropertyDrawer(typeof(ScriptableVariable<>), true)]
    public class ScriptableVariableDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();

            object val = SerializedPropertyHelper.GetPropertyValue(property, out Type propertyType);

            ObjectField variableField = new ObjectField(property.displayName);
            variableField.objectType = propertyType;
            variableField.AddToClassList("unity-base-field__aligned");
            variableField.BindProperty(property);
            root.Add(variableField);

            VariableValueField variableValueField = new VariableValueField(property);
            variableValueField.variable = property.objectReferenceValue;
            root.Add(variableValueField);

            variableField.RegisterValueChangedCallback(e =>
            {
                variableValueField.variable = property.objectReferenceValue;
            });

            return root;
        }
    
        private class VariableValueField : VisualElement
        {
            private UnityEngine.Object m_variable;

            private SerializedObject m_serializedVariable;
            private SerializedProperty m_variableValueProperty;

            public UnityEngine.Object variable
            {
                get => m_variable;
                set
                {
                    if (m_variable != value)
                    {
                        m_variable = value;
                        m_serializedVariable = m_variable == null ? null : new SerializedObject(m_variable);
                        m_variableValueProperty = m_serializedVariable?.FindProperty("m_currentValue");

                        m_foldout.Clear();
                        m_foldout.style.display = DisplayStyle.None;
                        if (m_variableValueProperty != null)
                        {
                            IMGUIContainer valueField = new IMGUIContainer(() =>
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.PropertyField(m_variableValueProperty);
                                EditorGUI.EndDisabledGroup();
                            });
                            m_foldout.Add(valueField);
                            m_foldout.style.display = DisplayStyle.Flex;
                        }
                    }
                }
            }

            private SerializedProperty m_property;

            private Foldout m_foldout;

            public VariableValueField(SerializedProperty property)
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
