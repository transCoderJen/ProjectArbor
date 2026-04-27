using System.Collections;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Tools
{

    [RequireComponent(typeof(Collider))]
    public class Occludable : MonoBehaviour
    {
        private static readonly int FadeAlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int ShadowClipThresholdId = Shader.PropertyToID("_ShadowClipThreshold");

        [Header("Fade Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float TransparentAlpha = 0.35f;
        [SerializeField] private float InvisibleAlpha = 0.00f;

        [SerializeField] private float FadeDuration = 0.2f;

        [Header("Shadow Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float VisibleShadowClipThreshold = 0.2f;

        [Range(0f, 1f)]
        [SerializeField] private float OccludedShadowClipThreshold = 1f;

        [Header("Visuals")]
        [SerializeField] private Renderer[] TargetRenderers;

        private Material[] materials;
        private Coroutine fadeCoroutine;
        private float currentTargetAlpha = 1f;
        private float currentTargetShadowClipThreshold = 0.5f;

        private void Awake()
        {
            if (TargetRenderers == null || TargetRenderers.Length == 0)
                TargetRenderers = GetComponentsInChildren<Renderer>();

            CacheMaterials();

            currentTargetShadowClipThreshold = VisibleShadowClipThreshold;

            ApplyImmediateValues(1f, VisibleShadowClipThreshold);
        }

        public void SetOccluded(bool isOccluded)
        {
            Camera cam = CameraManager.Instance?.CurrentCamera;

            bool forceInvisible = false;

            if (cam != null)
            {
                // Use renderer bounds center if available (better for tall objects like trees)
                Vector3 worldPoint = transform.position;

                if (TargetRenderers != null && TargetRenderers.Length > 0 && TargetRenderers[0] != null)
                {
                    worldPoint = TargetRenderers[0].bounds.center;
                }

                // Convert to camera local space
                Vector3 localPos = cam.transform.InverseTransformPoint(worldPoint);
                float depthToScreen = localPos.z;

                // Only consider objects in front of the camera
                if (depthToScreen > 0f)
                {
                    forceInvisible = depthToScreen < OcclusionManager.Instance.zDepthCutOff;
                }

                // Debug.Log($"{name} | DepthToScreen: {depthToScreen:F2} | ForceInvisible: {forceInvisible}");
            }

            float targetAlpha = forceInvisible
                ? 0f
                : (isOccluded ? TransparentAlpha : 1f);

            float targetShadowClipThreshold = forceInvisible
                ? 1f
                : (isOccluded ? OccludedShadowClipThreshold : VisibleShadowClipThreshold);

            bool alphaUnchanged = Mathf.Approximately(currentTargetAlpha, targetAlpha);
            bool shadowUnchanged = Mathf.Approximately(currentTargetShadowClipThreshold, targetShadowClipThreshold);

            if (alphaUnchanged && shadowUnchanged)
                return;

            currentTargetAlpha = targetAlpha;
            currentTargetShadowClipThreshold = targetShadowClipThreshold;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, targetShadowClipThreshold));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float targetShadowClipThreshold)
        {
            if (materials == null || materials.Length == 0)
                yield break;

            float elapsed = 0f;
            float[] startAlphas = new float[materials.Length];
            float[] startShadowThresholds = new float[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                startAlphas[i] = GetFadeAlpha(material);
                startShadowThresholds[i] = GetShadowClipThreshold(material);
            }

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeDuration);

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];

                    if (material == null)
                        continue;

                    float alpha = Mathf.Lerp(startAlphas[i], targetAlpha, t);
                    float shadowThreshold = Mathf.Lerp(startShadowThresholds[i], targetShadowClipThreshold, t);

                    SetFadeAlpha(material, alpha);
                    SetShadowClipThreshold(material, shadowThreshold);
                }

                yield return null;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                SetFadeAlpha(material, targetAlpha);
                SetShadowClipThreshold(material, targetShadowClipThreshold);
            }

            fadeCoroutine = null;
        }

        private float GetFadeAlpha(Material material)
        {
            if (material.HasProperty(FadeAlphaId))
                return material.GetFloat(FadeAlphaId);

            return 1f;
        }

        private void SetFadeAlpha(Material material, float alpha)
        {
            if (material.HasProperty(FadeAlphaId))
                material.SetFloat(FadeAlphaId, alpha);
        }

        private float GetShadowClipThreshold(Material material)
        {
            if (material.HasProperty(ShadowClipThresholdId))
                return material.GetFloat(ShadowClipThresholdId);

            return 0f;
        }

        private void SetShadowClipThreshold(Material material, float threshold)
        {
            if (material.HasProperty(ShadowClipThresholdId))
                material.SetFloat(ShadowClipThresholdId, threshold);
        }

        private void ApplyImmediateValues(float alpha, float shadowClipThreshold)
        {
            if (materials == null)
                return;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                SetFadeAlpha(material, alpha);
                SetShadowClipThreshold(material, shadowClipThreshold);
            }
        }

        private void CacheMaterials()
        {
            if (TargetRenderers == null || TargetRenderers.Length == 0)
            {
                materials = System.Array.Empty<Material>();
                return;
            }

            int materialCount = 0;

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                Renderer rendererComponent = TargetRenderers[i];

                if (rendererComponent == null)
                    continue;

                materialCount += rendererComponent.materials.Length;
            }

            materials = new Material[materialCount];

            int materialIndex = 0;

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                Renderer rendererComponent = TargetRenderers[i];

                if (rendererComponent == null)
                    continue;

                Material[] rendererMaterials = rendererComponent.materials;

                for (int j = 0; j < rendererMaterials.Length; j++)
                {
                    materials[materialIndex] = rendererMaterials[j];
                    materialIndex++;
                }
            }
        }
    }
}