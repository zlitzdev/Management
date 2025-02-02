using UnityEngine;

namespace Zlitz.General.Management
{
    public static class CachedValues
    {
        public static CachedValue<Camera> GetMainCameraCached()
        {
            return new CachedValue<Camera>(GetMainCamera, IsCameraNotMain);
        }

        public static CachedValue<T> GetComponentCached<T>(this GameObject gameObject) where T : Component
        {
            return new CachedValue<T>(gameObject.GetComponent<T>, IsObjectNull);
        }

        public static CachedValue<T> GetComponentCached<T>(this Component component) where T : Component
        {
            return new CachedValue<T>(component.GetComponent<T>, IsObjectNull);
        }


        private static Camera GetMainCamera() => Camera.main;


        private static bool IsObjectNull(UnityEngine.Object obj)
        {
            return obj == null;
        }

        private static bool IsCameraNotMain(Camera camera)
        {
            return camera == null || !camera.CompareTag("MainCamera");
        }
    }
}
