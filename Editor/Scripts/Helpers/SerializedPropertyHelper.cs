using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Zlitz.General.Management
{
    internal static class SerializedPropertyHelper
    {
        public static bool BelongsToScene(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            UnityEngine.Object target = property.serializedObject.targetObject;

            if (AssetDatabase.Contains(target))
            {
                return false;
            }

            GameObject gameObject = (target as GameObject) ?? (target as Component)?.gameObject;
            if (gameObject == null)
            {
                return false;
            }

            return gameObject.scene.IsValid();
        }

        public static bool IsPropertyArrayElement(SerializedProperty property, out int index, out string arrayPropertyPath)
        {
            index = -1;
            arrayPropertyPath = "";
            if (property == null)
            {
                return false;
            }

            string path = property.propertyPath;
            if (!path.EndsWith("]"))
            {
                return false;
            }

            int arrayStart = path.LastIndexOf("Array.data[", StringComparison.Ordinal);
            if (arrayStart == -1)
            {
                return false;
            }

            int indexStart = arrayStart + "Array.data[".Length;
            int indexEnd = path.IndexOf(']', indexStart);
            if (indexEnd == -1)
            {
                return false;
            }

            string indexString = path.Substring(indexStart, indexEnd - indexStart);
            if (!int.TryParse(indexString, out index))
            {
                return false;
            }

            arrayPropertyPath = path.Substring(0, arrayStart - 1);
            return true;
        }

        public static object GetPropertyValue(SerializedProperty property)
        {
            return GetPropertyValue(property, out Type propertyType);
        }

        public static object GetPropertyValue(SerializedProperty property, out Type propertyType)
        {
            propertyType = null;

            object targetObject = property.serializedObject.targetObject;

            string[] pathParts = property.propertyPath.Replace("Array.data[", "[").Split('.');

            foreach (var part in pathParts)
            {
                bool valid = false;
                if (part.Contains("["))
                {
                    int arrayIndex = ExtractArrayIndex(part);

                    if (GetArrayElement(targetObject, arrayIndex, out object value, out Type type))
                    {
                        targetObject = value;
                        propertyType = type;

                        valid = true;
                    } 
                }
                else
                {
                    if (GetPropertyValue(targetObject, part, out object value, out Type type))
                    {
                        targetObject = value;
                        propertyType = type;

                        valid = true;
                    }
                }

                if (!valid)
                {
                    Debug.LogError($"Property path '{property.propertyPath}' is invalid.");
                    propertyType = null;
                    return null;
                }
            }

            return targetObject;
        }

        public static SerializedProperty GetPropertyByPath(SerializedObject serializedObject, string path)
        {
            if (serializedObject == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            path = path.Replace("Array.data[", "[");

            List<string> pathSegments = path.Split('.').ToList();

            SerializedProperty property = serializedObject.FindProperty(pathSegments[0]);
            pathSegments.RemoveAt(0);

            if (property == null)
            {
                return null;
            }

            foreach (string segment in pathSegments)
            {
                if (segment.StartsWith("[") && segment.EndsWith("]"))
                {
                    string indexString = segment.Substring(1, segment.Length - 1);
                    if (int.TryParse(indexString, out int index))
                    {
                        if (property.isArray)
                        {
                            property = property.GetArrayElementAtIndex(index);
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    property = property.FindPropertyRelative(segment);
                    if (property == null)
                    {
                        return null;
                    }
                }
            }

            return property;
        }

        private static int ExtractArrayIndex(string arrayString)
        {
            int startIdx = arrayString.IndexOf('[') + 1;
            int endIdx = arrayString.IndexOf(']');
            string indexString = arrayString.Substring(startIdx, endIdx - startIdx);
            return int.Parse(indexString);
        }

        private static bool GetArrayElement(object targetObject, int index, out object value, out Type type)
        {
            value = null;
            type  = null;

            if (targetObject is IList list)
            {
                Type listType = list.GetType();

                type = typeof(object);
                if (listType.IsGenericType)
                {
                    type = listType.GetGenericArguments()[0];
                }
                else if (listType.IsArray)
                {
                    type = listType.GetElementType();
                }
                value = list[index];
                return true;
            }
            return false;
        }

        private static bool GetPropertyValue(object targetObject, string propertyName, out object value, out Type type)
        {
            value = null;
            type  = null;

            FieldInfo fieldInfo = targetObject.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                type  = fieldInfo.FieldType;
                value = fieldInfo.GetValue(targetObject);
                return true;
            }

            Debug.LogError($"Field '{propertyName}' not found on type {targetObject.GetType().Name}");
            return false;
        }
    }
}
