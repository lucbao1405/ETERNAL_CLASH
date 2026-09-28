using UnityEngine;

namespace EternalClash.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        // Clear static khi object chet: editor giu static qua cac lan play mode,
        // de Instance stale (fake-null) thi moi lenh Instance?. van chay vao
        // object da destroy (MissingReferenceException trong tool edit mode).
        protected virtual void OnDestroy()
        {
            if (Instance == this as T)
                Instance = null;
        }
    }
}
