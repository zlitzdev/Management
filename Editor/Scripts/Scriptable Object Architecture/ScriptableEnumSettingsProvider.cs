using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using static UnityEngine.CullingGroup;

namespace Zlitz.General.Management
{
    internal class ScriptableEnumSettingsProvider : SettingsProvider
    {
        private static readonly Dictionary<Type, Type> s_scriptableEnumTypes = new Dictionary<Type, Type>();

        internal static void ResetScriptableEnumTypes()
        {
            s_scriptableEnumTypes.Clear();
            foreach (Type type in AllTypes())
            {
                if (IsScriptableEnumType(type, out Type baseType))
                {
                    s_scriptableEnumTypes.TryAdd(type, baseType);
                }
            }
        }

        private static bool IsScriptableEnumType(Type type, out Type baseType)
        {
            Type genericScriptableEnumType = typeof(ScriptableEnum<,>);

            type = type.BaseType;
            while (type != null && type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == genericScriptableEnumType)
                {
                    baseType = type;
                    return true;
                }

                type = type.BaseType;
            }

            baseType = null;
            return false;
        }

        private static IEnumerable<Type> AllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract);
        }

        private SerializedObject m_serializedSettings;

        private SerializedProperty m_entriesProperty;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            ScriptableEnumManager settings = ScriptableEnumManager.IO.RetrieveFromProjectSettings();
            if (m_serializedSettings == null && settings != null)
            {
                m_serializedSettings = new SerializedObject(settings);

                m_entriesProperty = m_serializedSettings.FindProperty("m_entries");
            }

            VisualElement root = new VisualElement();
            root.style.marginLeft = 10.0f;
            rootElement.Add(root);
            rootElement = root;

            if (m_serializedSettings == null)
            {
                HelpBox helpBox = new HelpBox("No ScriptableEnumManager found.", HelpBoxMessageType.Error);
                rootElement.Add(helpBox);
                return;
            }

            Label settingTitle = new Label("Scriptable Enum");
            settingTitle.style.fontSize = 20.0f;
            settingTitle.style.marginBottom = 6.0f;
            settingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            rootElement.Add(settingTitle);

            VisualElement settingContent = new VisualElement();
            rootElement.Add(settingContent);

            foreach (KeyValuePair<Type, Type> scriptableEnumType in s_scriptableEnumTypes)
            {
                VisualElement group = CreateScriptableEnumGroup(scriptableEnumType.Key, scriptableEnumType.Value, settings);
                if (group == null)
                {
                    continue;
                }
                settingContent.Add(group);
            }
        }

        private VisualElement CreateScriptableEnumGroup(Type type, Type baseType, ScriptableEnumManager settings)
        {
            if (type == null || baseType == null)
            {
                return null;
            }

            Type idType = baseType.GetGenericArguments()[1];

            ScriptableObject[] enums = AssetDatabase
                .FindAssets($"t:{type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath(path, type))
                .Where(s => s != null)
                .OfType<ScriptableObject>()
                .ToArray();

            if (enums.Length <= 0)
            {
                return null;
            }

            ScriptableEnumGroupFoldout root = new ScriptableEnumGroupFoldout();
            root.text = string.IsNullOrEmpty(type.Namespace)
                ? $"{ObjectNames.NicifyVariableName(type.Name)} - {enums.Length} {(enums.Length == 1 ? "entry" : "entries")}"
                : $"{ObjectNames.NicifyVariableName(type.Name)} ({type.Namespace}) - {enums.Length} {(enums.Length == 1 ? "entry" : "entries")}";

            foreach (ScriptableObject enumObject in enums)
            {
                SerializedObject serializedEnum = new SerializedObject(enumObject);

                SerializedProperty idProperty = serializedEnum.FindProperty("m_id");
                idProperty.isExpanded = false;

                AlignedField alignedField = new AlignedField();
                root.Add(alignedField);

                root.onUpdateConflictState += () =>
                {
                    ScriptableEnumManager.IO.Save(settings);
                };
                IMGUIContainer idField = new IMGUIContainer(() =>
                {
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 128.0f;

                    bool isExpanded = idProperty.isExpanded;
                    idProperty.isExpanded = true;
                    float propertyHeight = EditorGUI.GetPropertyHeight(idProperty);
                    idProperty.isExpanded = isExpanded;

                    bool multipleLine = propertyHeight > 20.0f;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(idProperty, multipleLine ? (string.IsNullOrEmpty(idType.Namespace) ? new GUIContent($"ID ({idType.Name})") : new GUIContent($"ID ({idType.Name} - {idType.Namespace})")) : GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                    {
                        idProperty.serializedObject.ApplyModifiedProperties();
                        root.UpdateConflictState();
                    }

                    EditorGUIUtility.labelWidth = labelWidth;
                });
                alignedField.labelContainer.Add(idField);

                ObjectField enumField = new ObjectField();
                enumField.style.flexGrow = 1.0f;
                enumField.value = enumObject;
                enumField.SetEnabled(false);
                alignedField.fieldContainer.Add(enumField);

                ScriptableEnumOperation operation = new ScriptableEnumOperation(m_entriesProperty, enumObject, enums, root.UpdateConflictState);
                alignedField.fieldContainer.Add(operation);

                root.onUpdateConflictState += () => operation?.UpdateConflictState();
            }

            return root;
        }

        public ScriptableEnumSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateScriptableEnumSettingsProvider()
        {
            return new ScriptableEnumSettingsProvider("Project/Zlitz/Management/Scriptable Enum", SettingsScope.Project);
        }

        internal class AlignedField : VisualElement
        {
            public VisualElement labelContainer { get; private set; }

            public VisualElement fieldContainer { get; private set; }

            private float m_labelWidthRatio;
            private float m_labelExtraPadding;
            private float m_labelBaseMinWidth;
            private float m_labelExtraContextWidth;

            private VisualElement m_cachedContextWidthElement;
            private VisualElement m_cachedInspectorElement;

            public AlignedField()
            {
                style.flexDirection = FlexDirection.Row;
                style.minHeight = 20.0f;

                labelContainer = new VisualElement();
                labelContainer.style.flexDirection = FlexDirection.Column;
                labelContainer.style.justifyContent = Justify.Center;
                Add(labelContainer);

                fieldContainer = new VisualElement();
                fieldContainer.style.flexDirection = FlexDirection.Row;
                fieldContainer.style.flexGrow = 1.0f;
                fieldContainer.style.height = 20.0f;
                Add(fieldContainer);

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }

            private void OnAttachToPanel(AttachToPanelEvent e)
            {
                if (e.destinationPanel == null)
                {
                    return;
                }

                if (e.destinationPanel.contextType == ContextType.Player)
                {
                    return;
                }

                m_cachedInspectorElement = null;
                m_cachedContextWidthElement = null;

                var currentElement = parent;
                while (currentElement != null)
                {
                    if (currentElement.ClassListContains("unity-inspector-element"))
                    {
                        m_cachedInspectorElement = currentElement;
                    }

                    if (currentElement.ClassListContains("unity-inspector-main-container"))
                    {
                        m_cachedContextWidthElement = currentElement;
                        break;
                    }

                    currentElement = currentElement.parent;
                }

                if (m_cachedInspectorElement == null || m_cachedContextWidthElement == null)
                {
                    m_cachedInspectorElement = parent;
                    m_cachedContextWidthElement = parent;
                }

                if (m_cachedInspectorElement == null)
                {
                    RemoveFromClassList("unity-base-field__inspector-field");
                    return;
                }

                m_labelWidthRatio = 0.5f;

                m_labelExtraPadding = 37.0f;
                m_labelBaseMinWidth = 123.0f;

                m_labelExtraContextWidth = 1.0f;

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                AddToClassList("unity-base-field__inspector-field");
                RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
            }
            private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                AlignLabel();
            }

            private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
            {
                AlignLabel();
            }

            private void AlignLabel()
            {
                if (labelContainer == null)
                {
                    return;
                }

                float totalPadding = m_labelExtraPadding;
                float spacing = worldBound.x - m_cachedInspectorElement.worldBound.x - m_cachedInspectorElement.resolvedStyle.paddingLeft;

                totalPadding += spacing;
                totalPadding += resolvedStyle.paddingLeft;

                var minWidth = m_labelBaseMinWidth - spacing - resolvedStyle.paddingLeft;
                var contextWidthElement = m_cachedContextWidthElement ?? m_cachedInspectorElement;

                labelContainer.style.minWidth = Mathf.Max(minWidth, 0);

                var newWidth = (contextWidthElement.resolvedStyle.width + m_labelExtraContextWidth) * m_labelWidthRatio - totalPadding;
                if (Mathf.Abs(labelContainer.resolvedStyle.width - newWidth) > 1E-30f)
                {
                    labelContainer.style.width = Mathf.Max(0f, newWidth);
                }
            }
        }
    
        internal class ScriptableEnumOperation : VisualElement
        {
            private static readonly Texture2D s_icon = EditorGUIUtility.IconContent("console.infoicon@2x").image as Texture2D;

            private static readonly Color s_colorAdd         = new Color(0.2f, 0.5f, 0.3f);
            private static readonly Color s_colorRemove      = new Color(0.6f, 0.2f, 0.2f);
            private static readonly Color s_colorIncluded    = new Color(0.3f, 0.7f, 0.8f);
            private static readonly Color s_colorConflicted  = new Color(0.8f, 0.7f, 0.3f);
            private static readonly Color s_colorNotIncluded = new Color(0.4f, 0.4f, 0.4f);
            
            private SerializedProperty m_entriesProperty;
            private ScriptableObject   m_enumObject;
            private ScriptableObject[] m_groupObjects;

            private Action m_onStateChanged;

            private Button m_button;
            private Action m_buttonClick;

            private VisualElement m_icon;

            public void UpdateConflictState()
            {
                if (IsIncluded())
                {
                    bool conflicted = false;
                    foreach (ScriptableObject groupObject in m_groupObjects)
                    {
                        if (groupObject == m_enumObject || !IsIncluded(groupObject))
                        {
                            continue;
                        }
                        if (ScriptableEnumManager.CompareId(groupObject, m_enumObject))
                        {
                            conflicted = true;
                            break;
                        }
                    }
                    m_icon.style.unityBackgroundImageTintColor = conflicted ? s_colorConflicted : s_colorIncluded;
                    m_icon.tooltip = conflicted ? "Entries with duplicated ID will be ignored" : "Included";
                }
                else
                {
                    m_icon.style.unityBackgroundImageTintColor = s_colorNotIncluded;
                    m_icon.tooltip = "Not included";
                }
            }

            public ScriptableEnumOperation(SerializedProperty entriesProperty, ScriptableObject enumObject, ScriptableObject[] groupObjects, Action onStateChanged)
            {
                m_entriesProperty = entriesProperty;
                m_enumObject      = enumObject;
                m_groupObjects    = groupObjects;
                m_onStateChanged  = onStateChanged;

                style.width = 96.0f;
                style.flexDirection = FlexDirection.Row;

                m_button = new Button();
                m_button.style.flexGrow = 1.0f;
                m_button.clicked += OnButtonClick;
                Add(m_button);

                m_icon = new VisualElement();
                m_icon.style.backgroundImage = s_icon;
                m_icon.style.width  = 18.0f;
                m_icon.style.height = 18.0f;
                m_icon.style.marginLeft  = 2.0f;
                m_icon.style.marginRight = 8.0f;
                Add(m_icon);

                UpdateConflictState();
                UpdateState(IsIncluded());
            }

            private void UpdateState(bool isIncluded)
            {
                if (isIncluded)
                {
                    m_button.style.backgroundColor = s_colorRemove;
                    m_button.text = "Remove";

                    m_buttonClick = RemoveFromRegistry;
                }
                else
                {
                    m_button.style.backgroundColor = s_colorAdd;
                    m_button.text = "Add";

                    m_buttonClick = AddToRegistry;
                }
                m_onStateChanged?.Invoke();
            }

            private void OnButtonClick()
            {
                m_buttonClick?.Invoke();
            }

            private bool IsIncluded(ScriptableObject obj = null)
            {
                if (obj == null)
                {
                    obj = m_enumObject;
                }
                for (int i = 0; i < m_entriesProperty.arraySize; i++)
                {
                    SerializedProperty elementProperty = m_entriesProperty.GetArrayElementAtIndex(i);
                    if (elementProperty.objectReferenceValue is ScriptableObject other && obj == other)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void AddToRegistry()
            {
                if (IsIncluded())
                {
                    return;
                }

                int index = m_entriesProperty.arraySize;
                m_entriesProperty.InsertArrayElementAtIndex(index);

                SerializedProperty elementProperty = m_entriesProperty.GetArrayElementAtIndex(index);
                elementProperty.objectReferenceValue = m_enumObject;

                m_entriesProperty.serializedObject.ApplyModifiedProperties();

                UpdateState(true);
            }

            private void RemoveFromRegistry()
            {
                for (int i = 0; i < m_entriesProperty.arraySize; i++)
                {
                    SerializedProperty elementProperty = m_entriesProperty.GetArrayElementAtIndex(i);
                    if (elementProperty.objectReferenceValue is ScriptableObject obj && obj == m_enumObject)
                    {
                        m_entriesProperty.DeleteArrayElementAtIndex(i);
                        m_entriesProperty.serializedObject.ApplyModifiedProperties();

                        UpdateState(false);

                        return;
                    }
                }
            }
        }

        internal class ScriptableEnumGroupFoldout : Foldout
        {
            public event Action onUpdateConflictState;

            public void UpdateConflictState()
            {
                onUpdateConflictState?.Invoke();
            }
        }
    }

    [InitializeOnLoad]
    internal static class ScriptableEnumSettingsInitializer
    {
        static ScriptableEnumSettingsInitializer()
        {
            ScriptableEnumSettingsProvider.ResetScriptableEnumTypes();
        }
    }
}
