using System.Collections;
using UnityEngine;

namespace ShiftedSignal.Garden.Misc
{
    public class UnscaledInvoke : MonoBehaviour
    {
        // Custom Invoke method using unscaled time
        public void InvokeUnscaled(System.Action method, float delay)
        {
            StartCoroutine(InvokeUnscaledCoroutine(method, delay));
        }

        private IEnumerator InvokeUnscaledCoroutine(System.Action method, float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            method.Invoke();
        }
    }
}
