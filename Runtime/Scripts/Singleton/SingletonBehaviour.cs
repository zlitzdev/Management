using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T s_instance;

        private static readonly object s_lock = new object();

        public static T instance
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("Singleton Behaviours can only be accessed in play mode");
                    return null;
                }

                lock (s_lock)
                {
                    if (s_instance == null)
                    {
                        s_instance = FindObjectOfType<T>();

                        if (s_instance == null)
                        {
                            GameObject singletonObject = new GameObject(typeof(T).Name);

                            s_instance = singletonObject.AddComponent<T>();
                            s_instance.Setup();
                        }

                        if (s_instance?.dontDestroyOnLoad ?? false)
                        {
                            DontDestroyOnLoad(s_instance.gameObject);
                        }
                    }

                    return s_instance;
                }
            }
        }

        protected virtual bool dontDestroyOnLoad => false;

        protected virtual void Setup()
        {
        }
    }
}
