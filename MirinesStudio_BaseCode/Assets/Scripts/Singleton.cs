using UnityEngine;

namespace MirinesStudio
{
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        // ----- Param -----

        private static T instance = null;
        private static bool quitting;

        public static T Instance
        {
            get
            {
                if (quitting)
                    return null;

                if (instance == null)
                    instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

                return instance;
            }
        }

        // ----- Init -----

        public void Awake()
        {
            if (quitting)
            {
                Destroy(gameObject);
                return;
            }

            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (instance == null)
                instance = this as T;

            DontDestroyOnLoad(gameObject);
        }

        // ----- Set -----

        // ----- Get -----

        public static T GetInstance() { return instance; }

        // ----- Main -----

        // ----- Logic -----

        protected virtual void OnApplicationQuit()
        {
            quitting = true;
        }

        protected void OnDestroy()
        {
            // 내가 싱글턴이면 정리
            if (instance == this)
                instance = null;
        }
    }

    public class Singleton<T> where T : class, new()
    {
        // ----- Param -----

        private static readonly T _instance = new T();
        public static T Instance { get { return _instance; } }

        // ----- Init -----

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }
}
