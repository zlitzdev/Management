using UnityEngine;

namespace Zlitz.General.Management
{
    public static class Services
    {
        public static IServiceLocator Global()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Service Locators can only be accessed in play mode");
                return null;
            }
            return ServiceLocator.Global();
        }

        public static IServiceLocator Of(GameObject gameObject)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Service Locators can only be accessed in play mode");
                return null;
            }
            return ServiceLocator.Of(gameObject);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            ServiceLocator.Global().Clear();
        }
    }
}
