using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        [SerializeField, Tooltip("If multiple singleton objects are included, only one with the highest priority is included")]
        private int m_priority;

        public int priority => m_priority;

        public virtual bool includeInRegistry => true;

        public static T instance => ScriptableSingletonManager.instance?.Get<T>();

        internal static bool ShouldInclude(ScriptableObject obj)
        {
            if (obj is ScriptableSingleton<T> singletonObject)
            {
                return singletonObject.includeInRegistry;
            }
            return false;
        }

        internal static int GetPriority(ScriptableObject obj)
        {
            if (obj is ScriptableSingleton<T> singletonObject)
            {
                return singletonObject.priority;
            }
            return int.MinValue;
        }
    }
}
