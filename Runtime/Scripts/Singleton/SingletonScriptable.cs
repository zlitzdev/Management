using System.Linq;

using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class SingletonScriptable<T> : ScriptableObject where T : SingletonScriptable<T>
    {
        private static T s_instance;

        private static readonly object s_lock = new object();

        public static T instance
        {
            get
            {
                lock (s_lock)
                {
                    if (s_instance == null)
                    {
                        s_instance = Resources.LoadAll<T>("").FirstOrDefault();
                    }

                    return s_instance;
                }
            }
        }
    }
}
