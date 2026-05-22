using UnityEngine;

namespace ShiftedSignal.Garden.Misc
{
    /// <summary>
    /// Makes a GameObject persist across scene loads.
    /// </summary>
    public class PersistObject : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoadEnabled = true;

        protected virtual void Awake()
        {
            if (Application.isPlaying && dontDestroyOnLoadEnabled)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}