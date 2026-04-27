using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Tools;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftedSignal.Garden.Managers
{
    /// <summary>
    /// Detects foreground objects between the camera and player, then adjusts their sorting
    /// so objects whose pivots are closer to the camera render over the player.
    /// </summary>
    public class PlayerOverlapSortingManager : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask OverlapLayers;
        [SerializeField] private float CastRadius = 1.25f;
        [SerializeField] private float MaxDistancePadding = 0.25f;
        [SerializeField] private float PlayerPivotHeightOffset = 0f;

        [Header("Player Sorting")]
        [SerializeField] private SortingGroup PlayerSortingGroup;
        [SerializeField] private SpriteRenderer PlayerSpriteRenderer;
        [SerializeField] private int FallbackPlayerSortingOrder = 0;

        private Player player;
        private Camera currentCamera;

        private readonly HashSet<OverlapSortable> activeSortables = new();
        private readonly HashSet<OverlapSortable> previousSortables = new();

        private void Awake()
        {
            player = FindFirstObjectByType<Player>();

            if (player != null)
            {
                if (PlayerSortingGroup == null)
                    PlayerSortingGroup = player.GetComponentInChildren<SortingGroup>();

                if (PlayerSpriteRenderer == null)
                    PlayerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<Player>();

                if (player == null)
                    return;

                if (PlayerSortingGroup == null)
                    PlayerSortingGroup = player.GetComponentInChildren<SortingGroup>();

                if (PlayerSpriteRenderer == null)
                    PlayerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
            }

            if (CameraManager.Instance != null)
                currentCamera = CameraManager.Instance.CurrentCamera;

            if (currentCamera == null)
                return;

            UpdateOverlapSorting();
        }

        private void UpdateOverlapSorting()
        {
            previousSortables.Clear();

            foreach (OverlapSortable sortable in activeSortables)
                previousSortables.Add(sortable);

            activeSortables.Clear();

            Vector3 cameraPosition = currentCamera.transform.position;
            Vector3 playerPivotPosition = player.transform.position + (Vector3.up * PlayerPivotHeightOffset);
            Vector3 direction = playerPivotPosition - cameraPosition;
            float distance = direction.magnitude + MaxDistancePadding;

            if (distance <= 0.01f)
                return;

            RaycastHit[] hits = Physics.SphereCastAll(
                cameraPosition,
                CastRadius,
                direction.normalized,
                distance,
                OverlapLayers,
                QueryTriggerInteraction.Collide);

            DrawSphereCastDebug(cameraPosition, playerPivotPosition, CastRadius, Color.yellow);

            int playerSortingOrder = GetPlayerSortingOrder();
            float playerDepth = GetCameraRelativeDepth(playerPivotPosition);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                OverlapSortable sortable = hit.collider.GetComponentInParent<OverlapSortable>();

                if (sortable == null)
                    continue;

                if (!activeSortables.Add(sortable))
                    continue;

                float objectDepth = GetCameraRelativeDepth(sortable.GetWorldPivotPosition());
                bool isInFrontOfPlayer = objectDepth < playerDepth;

                if (isInFrontOfPlayer)
                    sortable.SetForeground(playerSortingOrder);
                else
                    sortable.RestoreDefaultSorting();

                previousSortables.Remove(sortable);
            }

            foreach (OverlapSortable sortable in previousSortables)
            {
                if (sortable != null)
                    sortable.RestoreDefaultSorting();
            }
        }

        private int GetPlayerSortingOrder()
        {
            if (PlayerSortingGroup != null)
                return PlayerSortingGroup.sortingOrder;

            if (PlayerSpriteRenderer != null)
                return PlayerSpriteRenderer.sortingOrder;

            return FallbackPlayerSortingOrder;
        }

        private float GetCameraRelativeDepth(Vector3 worldPosition)
        {
            Vector3 localPosition = currentCamera.transform.InverseTransformPoint(worldPosition);
            return localPosition.z;
        }

        private void DrawSphereCastDebug(Vector3 start, Vector3 end, float radius, Color color)
        {
            Debug.DrawLine(start, end, color);

            DrawCircle(start, radius, color);
            DrawCircle(end, radius, color);

            Vector3 forward = (end - start).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(forward, up).normalized;

            if (right == Vector3.zero)
                right = Vector3.right;

            up = Vector3.Cross(right, forward).normalized;

            // Draw side lines (tube edges)
            Debug.DrawLine(start + up * radius, end + up * radius, color);
            Debug.DrawLine(start - up * radius, end - up * radius, color);
            Debug.DrawLine(start + right * radius, end + right * radius, color);
            Debug.DrawLine(start - right * radius, end - right * radius, color);
        }

        private void DrawCircle(Vector3 center, float radius, Color color, int segments = 16)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + Vector3.forward * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

                Debug.DrawLine(prevPoint, nextPoint, color);
                prevPoint = nextPoint;
            }
        }
    }
}