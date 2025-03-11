using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR

using System.IO;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEditorInternal;

#endif

namespace Zlitz.General.Management
{
    public class ScriptableSingletonManager : ScriptableObject
    {
        private static ScriptableSingletonManager s_instance;

        public static ScriptableSingletonManager instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Retrieve();
                }
                return s_instance;
            }
        }

        private static ScriptableSingletonManager Retrieve()
        {
            #if UNITY_EDITOR
            return IO.RetrieveFromProjectSettings();
            #else
            return Resources.LoadAll<ScriptableSingletonManager>("").FirstOrDefault();
            #endif
        }

        private static readonly Dictionary<Type, Type> s_scriptableSingletonTypes = new Dictionary<Type, Type>();

        [SerializeField]
        private ScriptableObject[] m_entries;

        private readonly Dictionary<Type, ScriptableObject> m_registries = new Dictionary<Type, ScriptableObject>();

        internal T Get<T>() where T : ScriptableSingleton<T>
        {
            if (m_registries.TryGetValue(typeof(T), out ScriptableObject obj) && obj != null)
            {
                return obj as T;
            }
            return null;
        }

        private void OnReset()
        {
            m_registries.Clear();
            if (m_entries == null)
            {
                m_entries = new ScriptableObject[] { };
            }
            foreach (ScriptableObject entry in m_entries.OrderByDescending(GetPriority))
            {
                if (entry != null && TryGetScriptableSingletonType(entry, out Type type, out Type baseType) && ShouldInclude(entry))
                {
                    m_registries.TryAdd(type, entry);
                }
            }
        }

        internal void Resolve()
        {
            List<ScriptableObject> included = new List<ScriptableObject>();
            HashSet<Type> includedType = new HashSet<Type>();

            if (m_entries == null)
            {
                m_entries = new ScriptableObject[] { };
            }
            foreach (ScriptableObject entry in m_entries.OrderByDescending(GetPriority))
            {
                if (entry != null && TryGetScriptableSingletonType(entry, out Type type, out Type baseType) && ShouldInclude(entry))
                {
                    if (includedType.Add(type))
                    {
                        included.Add(entry);
                    }
                }
            }

            m_entries = included.ToArray();
        }

        private static bool ShouldInclude(ScriptableObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!IsScriptableSingletonType(obj.GetType(), out Type baseType))
            {
                return false;
            }
            MethodInfo shouldIncludeMethod = baseType.GetMethod("ShouldInclude", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (shouldIncludeMethod == null)
            {
                return false;
            }

            return (bool)shouldIncludeMethod.Invoke(null, new object[] { obj });
        }

        private static int GetPriority(ScriptableObject obj)
        {
            if (obj == null)
            {
                return int.MinValue;
            }
            if (!IsScriptableSingletonType(obj.GetType(), out Type baseType))
            {
                return int.MinValue;
            }
            MethodInfo getPriorityMethod = baseType.GetMethod("GetPriority", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getPriorityMethod == null)
            {
                return int.MinValue;
            }

            return (int)getPriorityMethod.Invoke(null, new object[] { obj });
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

        private static bool TryGetScriptableSingletonType(ScriptableObject obj, out Type type, out Type baseType)
        {
            if (obj != null)
            {
                Type objectType = obj.GetType();

                foreach (KeyValuePair<Type, Type> scriptableSingletonType in s_scriptableSingletonTypes)
                {
                    if (scriptableSingletonType.Key.IsAssignableFrom(objectType)) 
                    {
                        type = scriptableSingletonType.Key;
                        baseType = scriptableSingletonType.Value;
                        return true;
                    }
                }
            }
            
            type = null;
            baseType = null;
            return false;
        }

        private static IEnumerable<Type> AllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            s_scriptableSingletonTypes.Clear();
            foreach (Type type in AllTypes())
            {
                if (IsScriptableSingletonType(type, out Type baseType))
                {
                    s_scriptableSingletonTypes.TryAdd(type, baseType);
                }
            }

            ScriptableSingletonManager manager = instance;
            if (manager != null)
            {
                manager.OnReset();
            }
        }

        #if UNITY_EDITOR

        public static class IO
        {
            private static readonly Type s_scriptableSingletonManagerType = typeof(ScriptableSingletonManager);
            private static readonly string s_formattedName = FormatName(s_scriptableSingletonManagerType);
            private static readonly string s_savePath = SavePath(s_scriptableSingletonManagerType);

            private static ScriptableSingletonManager s_loaded;

            public static ScriptableSingletonManager loaded => s_loaded;

            public static ScriptableSingletonManager RetrieveFromProjectSettings()
            {
                ScriptableSingletonManager instance = Load();
                if (instance == null)
                {
                    instance = Create();
                }

                return instance;
            }

            public static void Save(ScriptableSingletonManager instance)
            {
                if (instance == null)
                {
                    return;
                }
                s_loaded = instance;
                InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { instance }, s_savePath, true);
            }

            private static ScriptableSingletonManager Load()
            {
                if (s_loaded != null)
                {
                    return s_loaded;
                }

                ScriptableSingletonManager instance = InternalEditorUtility.LoadSerializedFileAndForget(s_savePath).OfType<ScriptableSingletonManager>().FirstOrDefault();
                if (instance != null)
                {
                    s_loaded = instance;
                    instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                }

                return instance;
            }

            private static ScriptableSingletonManager Create()
            {
                ScriptableSingletonManager newInstance = CreateInstance<ScriptableSingletonManager>();
                newInstance.name = s_formattedName;
                newInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;

                s_loaded = newInstance;

                Save(newInstance);

                return newInstance;
            }

            private static string SavePath(Type type)
            {
                return $"ProjectSettings/{FormatName(type)}.asset";
            }

            private static string FormatName(Type type)
            {
                return type.FullName.Replace('.', '_');
            }

            static IO()
            {
                s_loaded = null;
                AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            }

            private static void BeforeAssemblyReload()
            {
                if (s_loaded != null)
                {
                    DestroyImmediate(s_loaded, true);
                    s_loaded = null;
                }
            }
        }

        #endif
    }

    #if UNITY_EDITOR

    internal class ScriptableSingletonBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static readonly Type s_scriptableSingletonManagerType = typeof(ScriptableSingletonManager);
        private static readonly string s_cachedPath = CachedPath(s_scriptableSingletonManagerType);

        public int callbackOrder => -200;

        public void OnPreprocessBuild(BuildReport report)
        {
            Directory.CreateDirectory("Assets/Resources");

            AssetDatabase.DeleteAsset(s_cachedPath);

            ScriptableSingletonManager instance = ScriptableSingletonManager.IO.RetrieveFromProjectSettings();
            instance = ScriptableSingletonManager.Instantiate(instance);
            instance.Resolve();

            HideFlags hideFlags = instance.hideFlags;
            instance.hideFlags = HideFlags.None;

            AssetDatabase.CreateAsset(instance, s_cachedPath);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            AssetDatabase.DeleteAsset(s_cachedPath);
            AssetDatabase.Refresh();
        }

        private static string CachedPath(Type type)
        {
            return $"Assets/Resources/{type.FullName.Replace('.', '_')}.asset";
        }
    }

    #endif    
}
