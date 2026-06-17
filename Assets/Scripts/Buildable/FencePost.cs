using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class FencePost2D : BaseBuildable
    {
        [Header("Fence 2D Sprites")]
        [SerializeField] private GameObject leftRightFence;
        [SerializeField] private GameObject northSouthFence;

        [Header("Fallback")]
        [SerializeField] private bool showLeftRightWhenAlone = true;

        [Header("Debug")]
        [SerializeField] private bool debugFence;

        protected override void OnEnable()
        {
            base.OnEnable();
            ShowDefaultVisual("OnEnable");
        }

        private void Start()
        {
            if (OccupiedBlock != null)
                RefreshConnections(OccupiedBlock);
            else
                ShowDefaultVisual("Start No OccupiedBlock");
        }

        public void RefreshConnections(GrowBlock block)
        {
            if (block == null)
            {
                ShowDefaultVisual("Null Block");
                return;
            }

            bool hasNorth = HasFencePost(block, Vector2Int.up);
            bool hasSouth = HasFencePost(block, Vector2Int.down);
            bool hasEast = HasFencePost(block, Vector2Int.right);
            bool hasWest = HasFencePost(block, Vector2Int.left);

            bool showNorthSouth = hasNorth || hasSouth;
            bool showLeftRight = hasEast || hasWest;

            if (!showNorthSouth && !showLeftRight)
                showLeftRight = showLeftRightWhenAlone;

            SetVisuals(showLeftRight, showNorthSouth);

            DebugState("RefreshConnections", block, hasNorth, hasSouth, hasEast, hasWest);
            EnsureAtLeastOneVisual("RefreshConnections");
        }

        public void RefreshGhostConnections(GrowBlock previewBlock)
        {
            RefreshConnections(previewBlock);
        }

        public void ShowDefaultVisual(string reason = "Default")
        {
            SetVisuals(showLeftRightWhenAlone, !showLeftRightWhenAlone);

            DebugState(reason);
            EnsureAtLeastOneVisual(reason);
        }

        private void SetVisuals(bool showLeftRight, bool showNorthSouth)
        {
            SetActive(leftRightFence, showLeftRight);
            SetActive(northSouthFence, showNorthSouth);
        }

        protected override void DestroyBuilding()
        {
            GrowBlock blockToRefreshFrom = OccupiedBlock;

            base.DestroyBuilding();

            if (blockToRefreshFrom != null)
                RefreshNeighbors(blockToRefreshFrom);
        }

        public static void RefreshNeighbors(GrowBlock centerBlock)
        {
            if (centerBlock == null || GridManager.Instance == null)
                return;

            RefreshFenceAt(centerBlock);
            RefreshFenceAt(GridManager.Instance.GetNorthNeighbor(centerBlock));
            RefreshFenceAt(GridManager.Instance.GetSouthNeighbor(centerBlock));
            RefreshFenceAt(GridManager.Instance.GetEastNeighbor(centerBlock));
            RefreshFenceAt(GridManager.Instance.GetWestNeighbor(centerBlock));
        }

        private static void RefreshFenceAt(GrowBlock block)
        {
            if (block == null)
                return;

            if (block.CurrentBuildable is FencePost2D fencePost)
                fencePost.RefreshConnections(block);
        }

        private bool HasFencePost(GrowBlock block, Vector2Int direction)
        {
            if (block == null || GridManager.Instance == null)
                return false;

            GrowBlock neighbor = GridManager.Instance.GetNeighbor(block, direction);

            if (neighbor == null)
                return false;

            return neighbor.CurrentBuildable is FencePost2D;
        }

        private void EnsureAtLeastOneVisual(string reason)
        {
            bool leftRightActive =
                leftRightFence != null && leftRightFence.activeSelf;

            bool northSouthActive =
                northSouthFence != null && northSouthFence.activeSelf;

            if (leftRightActive || northSouthActive)
                return;

            Debug.LogWarning(
                $"FencePost2D: Both visuals were off. Forcing default. Reason: {reason}. Object: {name}",
                this);

            SetVisuals(true, false);
        }

        private void SetActive(GameObject target, bool active)
        {
            if (target == null)
            {
                if (debugFence)
                    Debug.LogWarning($"FencePost2D: Missing visual reference on {name}.", this);

                return;
            }

            target.SetActive(active);
        }

        private void DebugState(
            string reason,
            GrowBlock block = null,
            bool hasNorth = false,
            bool hasSouth = false,
            bool hasEast = false,
            bool hasWest = false)
        {
            if (!debugFence)
                return;

            Debug.Log(
                $"FencePost2D Debug: {reason}\n" +
                $"Object: {name}\n" +
                $"Block: {(block != null ? block.name : "null")}\n" +
                $"Neighbors - N:{hasNorth} S:{hasSouth} E:{hasEast} W:{hasWest}\n" +
                $"LeftRight assigned:{leftRightFence != null}, active:{(leftRightFence != null && leftRightFence.activeSelf)}\n" +
                $"NorthSouth assigned:{northSouthFence != null}, active:{(northSouthFence != null && northSouthFence.activeSelf)}",
                this);
        }
    }
}