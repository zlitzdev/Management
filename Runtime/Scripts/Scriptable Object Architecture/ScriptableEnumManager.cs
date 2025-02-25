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
    public class ScriptableEnumManager : ScriptableObject
    {
        private static ScriptableEnumManager s_instance;

        public static ScriptableEnumManager instance
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

        public static bool CompareId(ScriptableObject obj1, ScriptableObject obj2)
        {
            if (obj1 == null || !IsScriptableEnumType(obj1.GetType(), out Type baseType1))
            {
                return false;
            }
            if (obj2 == null || !IsScriptableEnumType(obj2.GetType(), out Type baseType2))
            {
                return false;
            }
            if (baseType1 != baseType2)
            {
                return false;
            }

            MethodInfo compareMethodInfo = baseType1.GetMethod("CompareId", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (compareMethodInfo == null)
            {
                return false;
            }

            return (bool)compareMethodInfo.Invoke(null, new object[] { obj1, obj2 });
        }

        private static ScriptableEnumManager Retrieve()
        {
            #if UNITY_EDITOR
            return IO.RetrieveFromProjectSettings();
            #else
            return Resources.LoadAll<ScriptableEnumManager>("").FirstOrDefault();
            #endif
        }

        private static readonly Dictionary<Type, Type> s_scriptableEnumTypes = new Dictionary<Type, Type>();

        [SerializeField]
        private ScriptableObject[] m_entries;

        private readonly Dictionary<Type, RegistryBase> m_registries = new Dictionary<Type, RegistryBase>();

        internal T Get<T, TId>(TId id) where T : ScriptableEnum<T, TId> where TId : IComparable<TId>    
        {
            if (m_registries.TryGetValue(typeof(T), out RegistryBase registry) && registry != null)
            {
                return registry.Get(id) as T; 
            }

            return null;
        }

        internal IEnumerable<T> GetAll<T, TId>()
        {
            if (m_registries.TryGetValue(typeof(T), out RegistryBase registry) && registry != null)
            {
                return registry.GetAll().OfType<T>();
            }

            return null;
        }

        private void OnReset()
        {
            Type genericRegistryType = typeof(Registry<,>);

            m_registries.Clear();
            if (m_entries == null)
            {
                m_entries = new ScriptableObject[] { };
            }
            foreach (ScriptableObject entry in m_entries)
            {
                if (entry != null && TryGetScriptableEnumType(entry, out Type type, out Type baseType) && ShouldInclude(entry))
                {
                    // Get or create registry
                    if (!m_registries.TryGetValue(type, out RegistryBase registry))
                    {
                        Type registryType = genericRegistryType.MakeGenericType(baseType.GetGenericArguments());
                        registry = Activator.CreateInstance(registryType) as RegistryBase;

                        m_registries.Add(type, registry);
                    }

                    if (!registry.Register(entry))
                    {
                        Debug.LogWarning($"Failed to register {entry} of type {type.Name}.");
                    }
                }
            }
        }

        internal void Resolve()
        {
            m_entries = m_entries?.Where(ShouldInclude).ToArray();
        }

        private static bool ShouldInclude(ScriptableObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!IsScriptableEnumType(obj.GetType(), out Type baseType))
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

        private static bool TryGetScriptableEnumType(ScriptableObject obj, out Type type, out Type baseType)
        {
            if (obj != null)
            {
                Type objectType = obj.GetType();

                foreach (KeyValuePair<Type, Type> scriptableEnumType in s_scriptableEnumTypes)
                {
                    if (scriptableEnumType.Key.IsAssignableFrom(objectType)) 
                    {
                        type = scriptableEnumType.Key;
                        baseType = scriptableEnumType.Value;
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
            s_scriptableEnumTypes.Clear();
            foreach (Type type in AllTypes())
            {
                if (IsScriptableEnumType(type, out Type baseType))
                {
                    s_scriptableEnumTypes.TryAdd(type, baseType);
                }
            }

            ScriptableEnumManager manager = instance;
            if (manager != null)
            {
                manager.OnReset();
            }
        }

        #if UNITY_EDITOR

        public static class IO
        {
            private static readonly Type s_scriptableEnumManagerType = typeof(ScriptableEnumManager);
            private static readonly string s_formattedName = FormatName(s_scriptableEnumManagerType);
            private static readonly string s_savePath = SavePath(s_scriptableEnumManagerType);

            private static ScriptableEnumManager s_loaded;

            public static ScriptableEnumManager loaded => s_loaded;

            public static ScriptableEnumManager RetrieveFromProjectSettings()
            {
                ScriptableEnumManager instance = Load();
                if (instance == null)
                {
                    instance = Create();
                }

                return instance;
            }

            public static void Save()
            {
                ScriptableEnumManager instance = Retrieve();
                if (instance == null)
                {
                    return;
                }
                InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { instance }, s_savePath, true);
            }

            private static ScriptableEnumManager Load()
            {
                if (s_loaded != null)
                {
                    return s_loaded;
                }

                ScriptableEnumManager instance = InternalEditorUtility.LoadSerializedFileAndForget(s_savePath).OfType<ScriptableEnumManager>().FirstOrDefault();
                if (instance != null)
                {
                    s_loaded = instance;
                    instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                }

                return instance;
            }

            private static ScriptableEnumManager Create()
            {
                ScriptableEnumManager newInstance = CreateInstance<ScriptableEnumManager>();
                newInstance.name = s_formattedName;
                newInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;

                s_loaded = newInstance;
                Save();

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
                    DestroyImmediate(s_loaded);
                    s_loaded = null;
                }
            }
        }

        #endif
    }

    internal abstract class RegistryBase
    {
        public abstract bool Register(ScriptableObject obj);

        public abstract ScriptableObject Get(object id);

        public abstract IEnumerable<ScriptableObject> GetAll();
    }

    internal class Registry<T, TId> : RegistryBase where T : ScriptableEnum<T, TId> where TId : IComparable<TId>
    {
        private readonly Dictionary<TId, T> m_registry = new Dictionary<TId, T>();

        public override bool Register(ScriptableObject obj)
        {
            if (obj is T scriptableEnum)
            {
                return m_registry.TryAdd(scriptableEnum.id, scriptableEnum);
            }

            return false;
        }

        public override ScriptableObject Get(object id)
        {
            if (id is TId enumId && m_registry.TryGetValue(enumId, out T value))
            {
                return value;
            }

            return null;
        }

        public override IEnumerable<ScriptableObject> GetAll()
        {
            return m_registry.Values;
        }
    }

    #if UNITY_EDITOR

    internal class ScriptableEnumBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static readonly Type s_scriptableEnumManagerType = typeof(ScriptableEnumManager);
        private static readonly string s_cachedPath = CachedPath(s_scriptableEnumManagerType);

        public int callbackOrder => -200;

        public void OnPreprocessBuild(BuildReport report)
        {
            Directory.CreateDirectory("Assets/Resources");

            AssetDatabase.DeleteAsset(s_cachedPath);

            ScriptableEnumManager instance = ScriptableEnumManager.IO.RetrieveFromProjectSettings();
            instance = ScriptableEnumManager.Instantiate(instance);
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
