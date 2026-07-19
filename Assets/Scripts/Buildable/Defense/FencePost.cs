using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class FencePost2D : BaseBuilding
    {
        [Header("Fence 2D Sprites")]
        [SerializeField] private GameObject leftRightFence;
        [SerializeField] private GameObject northSouthFence;
        public virtual bool IsGate => false;

        [Header("Established Connections")]
        [SerializeField] private bool establishedLeftRight;
        [SerializeField] private bool establishedNorthSouth;

        [Header("Fallback")]
        [SerializeField] private bool showLeftRightWhenAlone = true;

        [Header("Debug")]
        [SerializeField] private bool debugFence;

        public bool IsShowingLeftRight =>
            leftRightFence != null && leftRightFence.activeSelf;

        public bool IsShowingNorthSouth =>
            northSouthFence != null && northSouthFence.activeSelf;

        public bool HasLeftRightConnection =>
            establishedLeftRight ||
            (leftRightFence != null && leftRightFence.activeSelf);

        public bool HasNorthSouthConnection =>
            establishedNorthSouth ||
            (northSouthFence != null && northSouthFence.activeSelf);

        protected override void OnEnable()
        {
            base.OnEnable();
            ShowDefaultVisual("OnEnable");
        }

        protected override void Start()
        {
            base.Start();

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

            bool currentlyNorthSouth = hasNorth || hasSouth;
            bool currentlyLeftRight = hasEast || hasWest;

            /*
            * Only permanent, placed fences remember connections.
            * Ghost fences have no OccupiedBlock, so previewing does not lock them.
            */
            if (OccupiedBlock != null)
            {
                if (currentlyNorthSouth)
                    establishedNorthSouth = true;

                if (currentlyLeftRight)
                    establishedLeftRight = true;
            }

            bool showNorthSouth =
                currentlyNorthSouth || establishedNorthSouth;

            bool showLeftRight =
                currentlyLeftRight || establishedLeftRight;

            // The fallback is visual only and is not remembered as a connection.
            if (!showNorthSouth && !showLeftRight)
                showLeftRight = showLeftRightWhenAlone;

            SetVisuals(showLeftRight, showNorthSouth);

            DebugState(
                "RefreshConnections",
                block,
                hasNorth,
                hasSouth,
                hasEast,
                hasWest);

            EnsureAtLeastOneVisual("RefreshConnections");
        }

        public void RefreshGhostConnections(GrowBlock previewBlock)
        {
            establishedLeftRight = false;
            establishedNorthSouth = false;
            RefreshConnections(previewBlock);
        }

        public bool CanPlaceFence(GrowBlock block)
        {
            if (block == null || GridManager.Instance == null)
                return false;

            bool hasNorth = HasFencePost(block, Vector2Int.up);
            bool hasSouth = HasFencePost(block, Vector2Int.down);
            bool hasEast = HasFencePost(block, Vector2Int.right);
            bool hasWest = HasFencePost(block, Vector2Int.left);

            bool candidateNorthSouth = hasNorth || hasSouth;
            bool candidateLeftRight = hasEast || hasWest;

            // An isolated fence uses its fallback visual direction.
            if (!candidateNorthSouth && !candidateLeftRight)
            {
                candidateLeftRight = showLeftRightWhenAlone;
                candidateNorthSouth = !showLeftRightWhenAlone;
            }

            // A gate itself cannot be placed as a corner.
            if (IsGate &&
                candidateNorthSouth &&
                candidateLeftRight)
            {
                LogPlacementFailure(
                    block,
                    "Gate cannot be placed on a corner.");

                return false;
            }

            // Placing here would connect north/south to these gates.
            if (WouldMakeGateCorner(
                    GetFencePost(block, Vector2Int.up),
                    addsNorthSouthConnection: true))
            {
                LogPlacementFailure(
                    block,
                    "Placement would turn the north gate into a corner.");

                return false;
            }

            if (WouldMakeGateCorner(
                    GetFencePost(block, Vector2Int.down),
                    addsNorthSouthConnection: true))
            {
                LogPlacementFailure(
                    block,
                    "Placement would turn the south gate into a corner.");

                return false;
            }

            // Placing here would connect left/right to these gates.
            if (WouldMakeGateCorner(
                    GetFencePost(block, Vector2Int.right),
                    addsNorthSouthConnection: false))
            {
                LogPlacementFailure(
                    block,
                    "Placement would turn the east gate into a corner.");

                return false;
            }

            if (WouldMakeGateCorner(
                    GetFencePost(block, Vector2Int.left),
                    addsNorthSouthConnection: false))
            {
                LogPlacementFailure(
                    block,
                    "Placement would turn the west gate into a corner.");

                return false;
            }

            return CanPlaceWithoutStacking(block);
        }

        private static bool WouldMakeGateCorner(
            FencePost2D neighbor,
            bool addsNorthSouthConnection)
        {
            if (neighbor == null || !neighbor.IsGate)
                return false;

            if (addsNorthSouthConnection)
            {
                // The new fence adds a north/south connection.
                // That is invalid if the gate already runs left/right.
                return neighbor.HasLeftRightConnection;
            }

            // The new fence adds a left/right connection.
            // That is invalid if the gate already runs north/south.
            return neighbor.HasNorthSouthConnection;
        }

        private void LogPlacementFailure(
            GrowBlock block,
            string reason)
        {
            if (!debugFence)
                return;

            Debug.Log(
                $"Fence placement rejected on " +
                $"{(block != null ? block.name : "null")}: {reason}",
                this);
        }

        /// <summary>
        /// Returns false if placing this fence would create a parallel,
        /// stacked fence row.
        /// </summary>
        public bool CanPlaceWithoutStacking(GrowBlock block)
        {
            if (block == null || GridManager.Instance == null)
                return false;

            bool hasNorth = HasFencePost(block, Vector2Int.up);
            bool hasSouth = HasFencePost(block, Vector2Int.down);
            bool hasEast = HasFencePost(block, Vector2Int.right);
            bool hasWest = HasFencePost(block, Vector2Int.left);

            bool candidateNorthSouth = hasNorth || hasSouth;
            bool candidateLeftRight = hasEast || hasWest;

            // An isolated fence uses its configured fallback direction.
            if (!candidateNorthSouth && !candidateLeftRight)
            {
                candidateLeftRight = showLeftRightWhenAlone;
                candidateNorthSouth = !showLeftRightWhenAlone;
            }

            // A left-right row cannot be stacked directly above or below
            // another left-right row.
            if (candidateLeftRight)
            {
                FencePost2D northFence =
                    GetFencePost(block, Vector2Int.up);

                FencePost2D southFence =
                    GetFencePost(block, Vector2Int.down);

                if (IsLeftRightFence(northFence) ||
                    IsLeftRightFence(southFence))
                {
                    LogStackingFailure(
                        block,
                        "left-right fence beside another left-right row");

                    return false;
                }
            }

            // A north-south row cannot be stacked directly to the left
            // or right of another north-south row.
            if (candidateNorthSouth)
            {
                FencePost2D eastFence =
                    GetFencePost(block, Vector2Int.right);

                FencePost2D westFence =
                    GetFencePost(block, Vector2Int.left);

                if (IsNorthSouthFence(eastFence) ||
                    IsNorthSouthFence(westFence))
                {
                    LogStackingFailure(
                        block,
                        "north-south fence beside another north-south row");

                    return false;
                }
            }

            return true;
        }

        public void ShowDefaultVisual(string reason = "Default")
        {
            SetVisuals(
                showLeftRightWhenAlone,
                !showLeftRightWhenAlone);

            DebugState(reason);
            EnsureAtLeastOneVisual(reason);
        }

        private void SetVisuals(
            bool showLeftRight,
            bool showNorthSouth)
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

            RefreshFenceAt(
                GridManager.Instance.GetNorthNeighbor(centerBlock));

            RefreshFenceAt(
                GridManager.Instance.GetSouthNeighbor(centerBlock));

            RefreshFenceAt(
                GridManager.Instance.GetEastNeighbor(centerBlock));

            RefreshFenceAt(
                GridManager.Instance.GetWestNeighbor(centerBlock));
        }

        private static void RefreshFenceAt(GrowBlock block)
        {
            if (block == null)
                return;

            if (block.CurrentBuildable is FencePost2D fencePost)
                fencePost.RefreshConnections(block);
        }

        private bool HasFencePost(
            GrowBlock block,
            Vector2Int direction)
        {
            return GetFencePost(block, direction) != null;
        }

        private FencePost2D GetFencePost(
            GrowBlock block,
            Vector2Int direction)
        {
            if (block == null || GridManager.Instance == null)
                return null;

            GrowBlock neighbor =
                GridManager.Instance.GetNeighbor(block, direction);

            if (neighbor == null)
                return null;

            return neighbor.CurrentBuildable as FencePost2D;
        }

        private static bool IsLeftRightFence(
            FencePost2D fence)
        {
            return fence != null && fence.IsShowingLeftRight;
        }

        private static bool IsNorthSouthFence(
            FencePost2D fence)
        {
            return fence != null && fence.IsShowingNorthSouth;
        }

        private void LogStackingFailure(
            GrowBlock block,
            string reason)
        {
            if (!debugFence)
                return;

            Debug.Log(
                $"Fence placement rejected on {block.name}: {reason}.",
                this);
        }

        private void EnsureAtLeastOneVisual(string reason)
        {
            bool leftRightActive =
                leftRightFence != null &&
                leftRightFence.activeSelf;

            bool northSouthActive =
                northSouthFence != null &&
                northSouthFence.activeSelf;

            if (leftRightActive || northSouthActive)
                return;

            Debug.LogWarning(
                $"FencePost2D: Both visuals were off. " +
                $"Forcing default. Reason: {reason}. Object: {name}",
                this);

            SetVisuals(true, false);
        }

        private void SetActive(GameObject target, bool active)
        {
            if (target == null)
            {
                if (debugFence)
                {
                    Debug.LogWarning(
                        $"FencePost2D: Missing visual reference on {name}.",
                        this);
                }

                return;
            }

            target.SetActive(active);
        }

        public virtual void ReplaceWith(BaseBuilding replacement)
        {
            if (OccupiedBlock == null)
            {
                Destroy(gameObject);
                return;
            }

            GrowBlock block = OccupiedBlock;

            // Clear our reference before we're destroyed.
            block.SetBuildable(null);

            // Remove this fence.
            Destroy(gameObject);

            // Assign the replacement.
            replacement.SetOccupiedBlock(block);
            block.SetBuildable(replacement);

            // Refresh the new piece.
            if (replacement is FencePost2D replacementFence)
            {
                replacementFence.RefreshConnections(block);
            }

            // Refresh all neighboring fences/gates.
            RefreshNeighbors(block);
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
                $"Neighbors - N:{hasNorth} S:{hasSouth} " +
                $"E:{hasEast} W:{hasWest}\n" +
                $"LeftRight assigned:{leftRightFence != null}, " +
                $"active:{IsShowingLeftRight}\n" +
                $"NorthSouth assigned:{northSouthFence != null}, " +
                $"active:{IsShowingNorthSouth}",
                this);
        }
    }
}