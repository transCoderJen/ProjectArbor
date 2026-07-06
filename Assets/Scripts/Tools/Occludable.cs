using System.Collections;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Tools
{
    [RequireComponent(typeof(Collider))]
    public class Occludable : MonoBehaviour
    {
        private static readonly int FadeAlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int FullAlphaDissolveFadeId = Shader.PropertyToID("_FullAlphaDissolveFade");

        [Header("Fade Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float TransparentAlpha = 0.6f;

        [SerializeField] private float FadeDuration = 0.2f;

        // [Header("Shadow Settings")]
        // [Range(0f, 1f)]
        // [SerializeField] private float VisibleShadowClipThreshold = 0.2f;

        // [Range(0f, 1f)]
        // [SerializeField] private float OccludedShadowClipThreshold = 1f;

        [Header("Visuals")]
        [SerializeField] private Renderer[] TargetRenderers;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine fadeCoroutine;

        private float currentAlpha = 1f;
        private float currentTargetAlpha = 1f;

        private void Awake()
        {
            if (TargetRenderers == null || TargetRenderers.Length == 0)
                TargetRenderers = GetComponentsInChildren<Renderer>();

            propertyBlock = new MaterialPropertyBlock();

            ApplyAlphaImmediate(1f);
        }

        public void SetOccluded(bool isOccluded)
        {
            Camera cam = CameraManager.Instance?.CurrentCamera;

            bool forceInvisible = false;

            if (cam != null)
            {
                Vector3 worldPoint = transform.position;

                if (TargetRenderers != null &&
                    TargetRenderers.Length > 0 &&
                    TargetRenderers[0] != null)
                {
                    worldPoint = TargetRenderers[0].bounds.center;
                }

                Vector3 localPos = cam.transform.InverseTransformPoint(worldPoint);
                float depthToScreen = localPos.z;

                if (depthToScreen > 0f)
                    forceInvisible = depthToScreen < OcclusionManager.Instance.zDepthCutOff;
            }

            float targetAlpha = forceInvisible
                ? 0f
                : isOccluded
                    ? TransparentAlpha
                    : 1f;

            if (Mathf.Approximately(currentTargetAlpha, targetAlpha))
                return;

            currentTargetAlpha = targetAlpha;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            float startAlpha = currentAlpha;
            float elapsed = 0f;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / FadeDuration);
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                ApplyAlphaImmediate(alpha);

                yield return null;
            }

            ApplyAlphaImmediate(targetAlpha);
            fadeCoroutine = null;
        }

        private void ApplyAlphaImmediate(float alpha)
        {
            currentAlpha = alpha;

            if (TargetRenderers == null)
                return;

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                Renderer rendererComponent = TargetRenderers[i];

                if (rendererComponent == null)
                    continue;

                rendererComponent.GetPropertyBlock(propertyBlock);

                propertyBlock.SetFloat(FullAlphaDissolveFadeId, alpha);
                propertyBlock.SetFloat(FadeAlphaId, alpha);

                rendererComponent.SetPropertyBlock(propertyBlock);
            }
        }
    }
}