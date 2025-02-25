using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.General.Management
{
    [CustomPropertyDrawer(typeof(ObservableVariable<>), true)]
    public class ObservableVariableDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            PropertyField propertyField = new PropertyField(property.FindPropertyRelative("m_value"));
            propertyField.label = property.displayName;

            bool readOnly = Application.isPlaying && SerializedPropertyHelper.BelongsToScene(property);
            propertyField.SetEnabled(!readOnly);

            return propertyField;
        }
    }
}
