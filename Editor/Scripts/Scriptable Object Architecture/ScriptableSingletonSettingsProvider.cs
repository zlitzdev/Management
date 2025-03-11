using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Zlitz.General.Management
{
    internal class ScriptableSingletonSettingsProvider : SettingsProvider
    {
        private static readonly Dictionary<Type, Type> s_scriptableSingletonTypes = new Dictionary<Type, Type>();

        internal static void ResetScriptableSingletonTypes()
        {
            s_scriptableSingletonTypes.Clear();
            foreach (Type type in AllTypes())
            {
                if (IsScriptableSingletonType(type, out Type baseType))
                {
                    s_scriptableSingletonTypes.TryAdd(type, baseType);
                }
            }
        }

        private static bool IsScriptableSingletonType(Type type, out Type baseType)
        {
            Type genericScriptableSingletonType = typeof(ScriptableSingleton<>);

            type = type.BaseType;
            while (type != null && type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == genericScriptableSingletonType)
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
            ScriptableSingletonManager settings = ScriptableSingletonManager.IO.RetrieveFromProjectSettings();
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
                HelpBox helpBox = new HelpBox("No ScriptableSingletonManager found.", HelpBoxMessageType.Error);
                rootElement.Add(helpBox);
                return;
            }

            Label settingTitle = new Label("Scriptable Singleton");
            settingTitle.style.fontSize = 20.0f;
            settingTitle.style.marginBottom = 6.0f;
            settingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            rootElement.Add(settingTitle);

            VisualElement settingContent = new VisualElement();
            rootElement.Add(settingContent);

            foreach (KeyValuePair<Type, Type> scriptableEnumType in s_scriptableSingletonTypes)
            {
                VisualElement group = CreateScriptableSingletonGroup(scriptableEnumType.Key, scriptableEnumType.Value, settings);
                if (group == null)
                {
                    continue;
                }
                settingContent.Add(group);
            }
        }

        private VisualElement CreateScriptableSingletonGroup(Type type, Type baseType, ScriptableSingletonManager settings)
        {
            if (type == null || baseType == null)
            {
                return null;
            }

            Type idType = baseType.GetGenericArguments()[0];

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

                SerializedProperty priorityProperty = serializedEnum.FindProperty("m_priority");

                AlignedField alignedField = new AlignedField();
                root.Add(alignedField);

                root.onChanged += () =>
                {
                    ScriptableSingletonManager.IO.Save(settings);
                };
                IMGUIContainer idField = new IMGUIContainer(() =>
                {
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 64.0f;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(priorityProperty, new GUIContent("Priority"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        priorityProperty.serializedObject.ApplyModifiedProperties();
                        root.UpdateChanges();
                    }

                    EditorGUIUtility.labelWidth = labelWidth;
                });
                alignedField.labelContainer.Add(idField);

                ObjectField singletonField = new ObjectField();
                singletonField.style.flexGrow = 1.0f;
                singletonField.value = enumObject;
                singletonField.SetEnabled(false);
                alignedField.fieldContainer.Add(singletonField);

                ScriptableSingletonOperation operation = new ScriptableSingletonOperation(m_entriesProperty, enumObject, enums, root.UpdateChanges);
                alignedField.fieldContainer.Add(operation);

                root.onChanged += () => operation?.UpdateChanges();
            }

            return root;
        }

        public ScriptableSingletonSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateScriptableSingletonSettingsProvider()
        {
            return new ScriptableSingletonSettingsProvider("Project/Zlitz/Management/Scriptable Singleton", SettingsScope.Project);
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
    
        internal class ScriptableSingletonOperation : VisualElement
        {
            private static readonly Color s_colorSet    = new Color(0.2f, 0.5f, 0.3f);
            private static readonly Color s_colorRemove = new Color(0.6f, 0.2f, 0.2f);
            
            private SerializedProperty m_entriesProperty;
            private ScriptableObject   m_singletonObject;
            private ScriptableObject[] m_groupObjects;

            private Action m_onStateChanged;

            private Button m_button;
            private Action m_buttonClick;

            public void UpdateChanges()
            {

            }

            public ScriptableSingletonOperation(SerializedProperty entriesProperty, ScriptableObject singletonObject, ScriptableObject[] groupObjects, Action onStateChanged)
            {
                m_entriesProperty = entriesProperty;
                m_singletonObject = singletonObject;
                m_groupObjects    = groupObjects;
                m_onStateChanged  = onStateChanged;

                style.width = 96.0f;
                style.flexDirection = FlexDirection.Row;

                m_button = new Button();
                m_button.style.flexGrow = 1.0f;
                m_button.clicked += OnButtonClick;
                Add(m_button);

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
                    m_button.style.backgroundColor = s_colorSet;
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
                    obj = m_singletonObject;
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
                elementProperty.objectReferenceValue = m_singletonObject;

                m_entriesProperty.serializedObject.ApplyModifiedProperties();

                UpdateState(true);
            }

            private void RemoveFromRegistry()
            {
                for (int i = 0; i < m_entriesProperty.arraySize; i++)
                {
                    SerializedProperty elementProperty = m_entriesProperty.GetArrayElementAtIndex(i);
                    if (elementProperty.objectReferenceValue is ScriptableObject obj && obj == m_singletonObject)
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
            public event Action onChanged;

            public void UpdateChanges()
            {
                onChanged?.Invoke();
            }
        }
    }

    [InitializeOnLoad]
    internal static class ScriptableSingletonSettingsInitializer
    {
        static ScriptableSingletonSettingsInitializer()
        {
            ScriptableSingletonSettingsProvider.ResetScriptableSingletonTypes();
        }
    }
}
