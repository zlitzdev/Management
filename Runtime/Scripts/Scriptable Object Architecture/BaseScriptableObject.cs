using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class BaseScriptableObject : ScriptableObject
    {
        private static readonly List<BaseScriptableObject> s_instances = new List<BaseScriptableObject>();

        protected virtual void OnReset()
        {
        }

        private void OnEnable()
        {
            s_instances.Add(this);
        }

        private void OnDisable()
        {
            s_instances.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAll()
        {
            foreach (BaseScriptableObject @object in s_instances)
            {
                @object?.OnReset();
            }
        }
    }
}
