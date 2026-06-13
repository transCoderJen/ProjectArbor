using System.Collections;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_HeartSlot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject FilledHeart;

        [Header("Pop Out")]
        [SerializeField] private float PopOutDuration = 0.18f;
        [SerializeField] private float PopOutScale = 1.35f;

        [Header("Pop In")]
        [SerializeField] private float PopInDuration = 0.14f;
        [SerializeField] private float PopInStartScale = 0.65f;
        [SerializeField] private float PopInOvershootScale = 1.2f;

        private Coroutine animationRoutine;

        public void Show(bool animate)
        {
            if (FilledHeart == null)
                return;

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            FilledHeart.SetActive(true);

            if (animate)
                animationRoutine = StartCoroutine(PopInRoutine());
            else
                FilledHeart.transform.localScale = Vector3.one;
        }

        public void Hide(bool animate)
        {
            if (FilledHeart == null)
                return;

            if (!FilledHeart.activeSelf)
                return;

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            if (animate)
                animationRoutine = StartCoroutine(PopOutRoutine());
            else
            {
                FilledHeart.SetActive(false);
                FilledHeart.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator PopOutRoutine()
        {
            float timer = 0f;

            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one * PopOutScale;

            while (timer < PopOutDuration)
            {
                timer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(timer / PopOutDuration);
                FilledHeart.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            FilledHeart.SetActive(false);
            FilledHeart.transform.localScale = Vector3.one;
            animationRoutine = null;
        }

        private IEnumerator PopInRoutine()
        {
            float timer = 0f;

            FilledHeart.transform.localScale = Vector3.one * PopInStartScale;

            while (timer < PopInDuration)
            {
                timer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(timer / PopInDuration);

                float scale;

                if (t < 0.65f)
                {
                    float firstT = t / 0.65f;
                    scale = Mathf.Lerp(PopInStartScale, PopInOvershootScale, firstT);
                }
                else
                {
                    float secondT = (t - 0.65f) / 0.35f;
                    scale = Mathf.Lerp(PopInOvershootScale, 1f, secondT);
                }

                FilledHeart.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            FilledHeart.transform.localScale = Vector3.one;
            animationRoutine = null;
        }
    }
}