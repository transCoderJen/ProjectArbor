using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftedSignal.Garden.Tools
{
    /// <summary>
    /// Stores sorting data for an object that may need to render in front of the player
    /// when its sprite pivot is closer to the camera than the player's pivot.
    /// </summary>
    public class OverlapSortable : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer[] TargetRenderers;
        [SerializeField] private SortingGroup TargetSortingGroup;

        [Header("Sorting")]
        [SerializeField] private int ForegroundOffset = 10;

        private int[] defaultSortingOrders;
        private bool hasCachedDefaults;

        private void Awake()
        {
            if (TargetSortingGroup == null)
                TargetSortingGroup = GetComponentInChildren<SortingGroup>();

            if (TargetRenderers == null || TargetRenderers.Length == 0)
                TargetRenderers = GetComponentsInChildren<SpriteRenderer>();

            CacheDefaultSorting();
        }

        /// <summary>
        /// Gets the best world-space pivot point for depth comparison.
        /// </summary>
        public Vector3 GetWorldPivotPosition()
        {
            SpriteRenderer pivotRenderer = GetPrimaryRenderer();

            if (pivotRenderer == null || pivotRenderer.sprite == null)
                return transform.position;

            Sprite sprite = pivotRenderer.sprite;

            Vector2 pivotPixels = sprite.pivot;
            Vector2 rectSizePixels = sprite.rect.size;
            float pixelsPerUnit = sprite.pixelsPerUnit;

            Vector2 pivotOffsetFromCenterPixels = pivotPixels - (rectSizePixels * 0.5f);
            Vector3 localPivotOffset = new Vector3(
                pivotOffsetFromCenterPixels.x / pixelsPerUnit,
                pivotOffsetFromCenterPixels.y / pixelsPerUnit,
                0f);

            Vector3 scaledLocalOffset = Vector3.Scale(localPivotOffset, pivotRenderer.transform.lossyScale);

            return pivotRenderer.transform.position + pivotRenderer.transform.rotation * scaledLocalOffset;
        }

        /// <summary>
        /// Sets this object to render in front of the player.
        /// </summary>
        public void SetForeground(int playerSortingOrder)
        {
            CacheDefaultSorting();

            if (TargetSortingGroup != null)
            {
                TargetSortingGroup.sortingOrder = playerSortingOrder + ForegroundOffset;
                return;
            }

            if (TargetRenderers == null)
                return;

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                if (TargetRenderers[i] == null)
                    continue;

                TargetRenderers[i].sortingOrder = playerSortingOrder + ForegroundOffset;
            }
        }

        /// <summary>
        /// Restores the object's original sorting order values.
        /// </summary>
        public void RestoreDefaultSorting()
        {
            if (!hasCachedDefaults)
                CacheDefaultSorting();

            if (TargetSortingGroup != null)
            {
                TargetSortingGroup.sortingOrder = defaultSortingOrders[0];
                return;
            }

            if (TargetRenderers == null || defaultSortingOrders == null)
                return;

            int count = Mathf.Min(TargetRenderers.Length, defaultSortingOrders.Length);

            for (int i = 0; i < count; i++)
            {
                if (TargetRenderers[i] == null)
                    continue;

                TargetRenderers[i].sortingOrder = defaultSortingOrders[i];
            }
        }

        private SpriteRenderer GetPrimaryRenderer()
        {
            if (TargetRenderers == null || TargetRenderers.Length == 0)
                return null;

            SpriteRenderer bestRenderer = null;
            int bestSortingOrder = int.MinValue;

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                SpriteRenderer currentRenderer = TargetRenderers[i];

                if (currentRenderer == null || currentRenderer.sprite == null)
                    continue;

                if (bestRenderer == null || currentRenderer.sortingOrder > bestSortingOrder)
                {
                    bestRenderer = currentRenderer;
                    bestSortingOrder = currentRenderer.sortingOrder;
                }
            }

            return bestRenderer;
        }

        private void CacheDefaultSorting()
        {
            if (hasCachedDefaults)
                return;

            if (TargetSortingGroup != null)
            {
                defaultSortingOrders = new[] { TargetSortingGroup.sortingOrder };
                hasCachedDefaults = true;
                return;
            }

            if (TargetRenderers == null || TargetRenderers.Length == 0)
            {
                defaultSortingOrders = System.Array.Empty<int>();
                hasCachedDefaults = true;
                return;
            }

            defaultSortingOrders = new int[TargetRenderers.Length];

            for (int i = 0; i < TargetRenderers.Length; i++)
            {
                defaultSortingOrders[i] = TargetRenderers[i] != null
                    ? TargetRenderers[i].sortingOrder
                    : 0;
            }

            hasCachedDefaults = true;
        }
    }
}