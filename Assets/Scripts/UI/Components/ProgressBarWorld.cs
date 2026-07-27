using System.Collections.Generic;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    public class ProgressBarWorld : MonoBehaviour
    {
        private static readonly List<ProgressBarWorld> ActiveBars = new();

        [Header("Progress")]
        [SerializeField] private RectTransform mask;
        [SerializeField] private Vector2 padding;

        [Header("World Position")]
        [SerializeField] private Transform target;
        [SerializeField] private float verticalOffset = 0.25f;

        [Header("Overlap Stagger")]
        [SerializeField] private float staggerYOffset = 0.35f;
        [SerializeField] private int maxStaggerLevel = 3;

        private RectTransform rectTransform;
        private RectTransform maskParentRectTransform;
        private Renderer[] targetRenderers;
        private int staggerLevel;

        private readonly Vector3[] worldCorners = new Vector3[4];

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            AssignStaggerLevel();

            if (!ActiveBars.Contains(this))
                ActiveBars.Add(this);
        }

        private void Initialize()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (mask == null)
            {
                Debug.LogError(
                    $"Progress bar {name} is missing a mask!",
                    this);

                return;
            }

            if (maskParentRectTransform == null)
                maskParentRectTransform =
                    mask.parent.GetComponent<RectTransform>();

            if (target == null)
                target = transform.parent;

            CacheTargetRenderers();
        }

        private void OnDisable()
        {
            ActiveBars.Remove(this);
            staggerLevel = 0;
        }

        private void LateUpdate()
        {
            FaceCamera();
            UpdateWorldPosition();
        }

        private void CacheTargetRenderers()
        {
            if (target == null)
            {
                targetRenderers = null;

                Debug.LogWarning(
                    $"Progress bar {name} has no target.",
                    this);

                return;
            }

            targetRenderers =
                target.GetComponentsInChildren<Renderer>(true);

            if (targetRenderers.Length == 0)
            {
                Debug.LogWarning(
                    $"Progress bar {name} found no renderers under target " +
                    $"{target.name}. It will use the target position instead.",
                    this);
            }
        }

        private void FaceCamera()
        {
            if (Helpers.Camera == null)
                return;

            transform.forward = Helpers.Camera.transform.forward;
        }


        private void UpdateWorldPosition()
        {
            if (target == null)
                return;

            Bounds bounds = GetTargetBounds();

            transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + verticalOffset + staggerLevel * staggerYOffset,
                bounds.center.z
            );
        }

        private void AssignStaggerLevel()
        {
            for (int level = 0; level <= maxStaggerLevel; level++)
            {
                bool levelIsUsed = false;

                foreach (ProgressBarWorld otherBar in ActiveBars)
                {
                    if (otherBar == null || otherBar == this)
                        continue;

                    if (!OverlapsOnX(otherBar))
                        continue;

                    if (otherBar.staggerLevel == level)
                    {
                        levelIsUsed = true;
                        break;
                    }
                }

                if (!levelIsUsed)
                {
                    staggerLevel = level;
                    return;
                }
            }

            staggerLevel = maxStaggerLevel;
        }

        private bool OverlapsOnX(ProgressBarWorld otherBar)
        {
            GetWorldXRange(out float myLeft, out float myRight);
            otherBar.GetWorldXRange(out float otherLeft, out float otherRight);

            return myLeft <= otherRight && myRight >= otherLeft;
        }

        private void GetWorldXRange(out float left, out float right)
        {
            if (rectTransform == null)
            {
                left = transform.position.x;
                right = transform.position.x;
                return;
            }

            rectTransform.GetWorldCorners(worldCorners);

            left = worldCorners[0].x;
            right = worldCorners[0].x;

            for (int i = 1; i < worldCorners.Length; i++)
            {
                left = Mathf.Min(left, worldCorners[i].x);
                right = Mathf.Max(right, worldCorners[i].x);
            }
        }

        private Bounds GetTargetBounds()
        {
            if (target == null)
                return new Bounds(transform.position, Vector3.one);

            if (targetRenderers == null || targetRenderers.Length == 0)
                return new Bounds(target.position, Vector3.one);

            bool foundRenderer = false;
            Bounds combinedBounds = default;

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                    continue;

                if (targetRenderer.transform.IsChildOf(transform))
                    continue;

                Bounds rendererBounds = targetRenderer.bounds;

                if (!foundRenderer)
                {
                    combinedBounds = rendererBounds;
                    foundRenderer = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rendererBounds);
                }
            }

            return foundRenderer
                ? combinedBounds
                : new Bounds(target.position, Vector3.one);
        }

        public void SetProgress(float progress)
        {
            if (maskParentRectTransform == null)
                return;

            Vector2 parentSize = maskParentRectTransform.sizeDelta;
            Vector2 targetSize = parentSize;

            targetSize.x *= Mathf.Clamp01(progress);

            mask.offsetMin = Vector2.zero + padding;
            mask.offsetMax = targetSize - parentSize - padding;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            CacheTargetRenderers();
        }

    }
}