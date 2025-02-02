using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class RegisterableScriptable<T, TId> : ScriptableObject where T : RegisterableScriptable<T, TId>
    {
        private static readonly Dictionary<TId, T> s_entries = new Dictionary<TId, T>();

        protected abstract TId id { get; }

        protected virtual bool shouldRegister => true;

        protected virtual void OnRegistered()
        {
        }

        public static IEnumerable<T> entries => s_entries.Values;

        public static T Get(TId id)
        {
            if (!s_entries.TryGetValue(id, out T entry))
            {
                entry = null;
            }
            return entry;
        }

        public static CachedValue<T> GetCached(TId id)
        {
            return new CachedValue<T>(() => Get(id));
        }

        internal static void Load()
        {
            s_entries.Clear();

            T[] entries = Resources.LoadAll<T>("");
            foreach (T entry in entries)
            {
                if (!entry.shouldRegister)
                {
                    continue;
                }

                if (!s_entries.TryAdd(entry.id, entry))
                {
                    Debug.LogWarning($"[{typeof(T).Name}] Repeated ID: {entry.id}");
                }
            }
        }
        
        internal static void PostLoad()
        {
            foreach (T entry in s_entries.Values)
            {
                entry?.OnRegistered();
            }
        }
    }
}
