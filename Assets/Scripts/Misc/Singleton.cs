using UnityEngine;

namespace ShiftedSignal.Garden.Misc
{
    
    /// <summary>
    /// Simple generic singleton base for runtime managers.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static T Instance => instance;

        [SerializeField] private bool DontDestroyOnLoadEnabled = true;

        protected virtual void Awake()
        {
            if (instance != null && instance != this as T)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;

            if (Application.isPlaying && DontDestroyOnLoadEnabled)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this as T)
            {
                instance = null;
            }
        }
    }
}