using System;
using System.Linq;
using System.Reflection;
using System.Collections;

using UnityEngine;
using UnityEditor;

namespace Zlitz.General.Management
{
    [InitializeOnLoad]
    internal static class ScriptIcons
    {
        private static readonly Type s_scriptableObjectType          = typeof(ScriptableObject);
        private static readonly Type s_genericScriptableVariableType = typeof(ScriptableVariable<>);
        private static readonly Type s_genericScriptableListType     = typeof(ScriptableList<>);
        private static readonly Type s_genericScriptableEventType    = typeof(ScriptableEvent<>);

        private static Texture2D s_scriptableVariableIcon;
        private static Texture2D s_scriptableListIcon;
        private static Texture2D s_scriptableEventIcon;

        private static MethodInfo s_getAnnotationsMethod;
        private static MethodInfo s_setGizmoEnabledMethod;
        private static MethodInfo s_setIconEnabledMethod;

        private static PropertyInfo s_classIdProperty;
        private static PropertyInfo s_scriptClassProperty;

        static ScriptIcons()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    if (s_scriptableObjectType.IsAssignableFrom(type) && IsScriptableVariableType(type))
                    {
                        if (s_scriptableVariableIcon == null)
                        {
                            s_scriptableVariableIcon = Resources.Load<Texture2D>("Icon_ScriptableVariable");
                        }

                        if (s_scriptableVariableIcon != null)
                        {
                            MonoScript monoScript = MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(script => script.GetClass() == type);
                            if (monoScript != null)
                            {
                                MonoImporter monoImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(monoScript)) as MonoImporter;
                                if (monoImporter != null)
                                {
                                    SetGizmoIconEnabled(type, false);
                                    if (monoImporter.GetIcon() == null)
                                    {
                                        monoImporter.SetIcon(s_scriptableVariableIcon);
                                        monoImporter.SaveAndReimport();
                                    }
                                }
                            }
                        }
                    }

                    if (s_scriptableObjectType.IsAssignableFrom(type) && IsScriptableListType(type))
                    {
                        if (s_scriptableListIcon == null)
                        {
                            s_scriptableListIcon = Resources.Load<Texture2D>("Icon_ScriptableList");
                        }

                        if (s_scriptableListIcon != null)
                        {
                            MonoScript monoScript = MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(script => script.GetClass() == type);
                            if (monoScript != null)
                            {
                                MonoImporter monoImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(monoScript)) as MonoImporter;
                                if (monoImporter != null)
                                {
                                    SetGizmoIconEnabled(type, false);
                                    if (monoImporter.GetIcon() == null)
                                    {
                                        monoImporter.SetIcon(s_scriptableListIcon);
                                        monoImporter.SaveAndReimport();
                                    }
                                }
                            }
                        }
                    }

                    if (s_scriptableObjectType.IsAssignableFrom(type) && IsScriptableEventType(type))
                    {
                        if (s_scriptableEventIcon == null)
                        {
                            s_scriptableEventIcon = Resources.Load<Texture2D>("Icon_ScriptableEvent");
                        }

                        if (s_scriptableEventIcon != null)
                        {
                            MonoScript monoScript = MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(script => script.GetClass() == type);
                            if (monoScript != null)
                            {
                                MonoImporter monoImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(monoScript)) as MonoImporter;
                                if (monoImporter != null)
                                {
                                    SetGizmoIconEnabled(type, false);
                                    if (monoImporter.GetIcon() == null)
                                    {
                                        monoImporter.SetIcon(s_scriptableEventIcon);
                                        monoImporter.SaveAndReimport();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    
        private static bool IsScriptableVariableType(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == s_genericScriptableVariableType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool IsScriptableListType(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == s_genericScriptableListType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool IsScriptableEventType(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == s_genericScriptableEventType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static void SetGizmoIconEnabled(Type type, bool on)
        {
            if (s_getAnnotationsMethod == null || s_classIdProperty == null || s_scriptClassProperty == null)
            {
                Type annotationType        = Type.GetType("UnityEditor.Annotation, UnityEditor");
                Type annotationUtilityType = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");

                if (annotationUtilityType != null && annotationType != null)
                {
                    s_getAnnotationsMethod  = annotationUtilityType.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
                    s_setGizmoEnabledMethod = annotationUtilityType.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                    s_setIconEnabledMethod  = annotationUtilityType.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

                    s_classIdProperty     = annotationType.GetProperty("classID", BindingFlags.Public | BindingFlags.Instance);
                    s_scriptClassProperty = annotationType.GetProperty("scriptClass", BindingFlags.Public | BindingFlags.Instance);
                }
            }

            if (s_getAnnotationsMethod == null || s_classIdProperty == null || s_scriptClassProperty == null)
            {
                return;
            }

            IEnumerable annotations = (IEnumerable)s_getAnnotationsMethod.Invoke(null, null);
            if (annotations == null)
            {
                return;
            }

            foreach (object annotation in annotations)
            {
                int    classId     = (int)s_classIdProperty.GetValue(annotation, null);
                string scriptClass = (string)s_scriptClassProperty.GetValue(annotation, null);
            
                if (scriptClass == type.Name)
                {
                    s_setGizmoEnabledMethod?.Invoke(null, new object[] 
                    {
                        classId,
                        scriptClass,
                        on ? 1 : 0
                    });
                    s_setIconEnabledMethod?.Invoke(null, new object[]
                    {
                        classId,
                        scriptClass,
                        on ? 1 : 0
                    });
                }
            }
        }
    }
}
