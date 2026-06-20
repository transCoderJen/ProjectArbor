using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class FencePost : BaseBuildable
    {
        [Header("Fence Connections")]
        [SerializeField] private GameObject northRail;
        [SerializeField] private GameObject southRail;
        [SerializeField] private GameObject eastRail;
        [SerializeField] private GameObject westRail;

        [Header("Damage Visuals")]
        [SerializeField] private GameObject[] damagePieces;

        private bool firstDamageStageTriggered;
        private bool secondDamageStageTriggered;

        public void RefreshConnections(GrowBlock block)
        {
            SetRailActive(northRail, HasFencePost(block, Vector2Int.up));
            SetRailActive(southRail, HasFencePost(block, Vector2Int.down));
            SetRailActive(eastRail, HasFencePost(block, Vector2Int.right));
            SetRailActive(westRail, HasFencePost(block, Vector2Int.left));
        }

        public void RefreshGhostConnections(GrowBlock previewBlock)
        {
            RefreshConnections(previewBlock);
        }

        public override void DoDamage(int damage)
        {
            base.DoDamage(damage);

            if (CurrentHealth <= 0)
                return;

            if (CurrentHealth <= MaxHealth * 0.5f && !firstDamageStageTriggered)
            {
                DestroyRandomActiveRail();
                DestroyDamagePiece(0);
                firstDamageStageTriggered = true;
            }

            if (CurrentHealth <= MaxHealth * 0.25f && !secondDamageStageTriggered)
            {
                DestroyRandomActiveRail();
                DestroyDamagePiece(1);
                secondDamageStageTriggered = true;
            }
        }

        protected override void DestroyBuilding()
        {
            GrowBlock blockToRefreshFrom = OccupiedBlock;

            base.DestroyBuilding();

            if (blockToRefreshFrom != null)
                RefreshNeighborFencePosts(blockToRefreshFrom);
        }

        private void RefreshNeighborFencePosts(GrowBlock block)
        {
            TryRefreshFencePost(block, Vector2Int.up);
            TryRefreshFencePost(block, Vector2Int.down);
            TryRefreshFencePost(block, Vector2Int.left);
            TryRefreshFencePost(block, Vector2Int.right);
        }

        public static void RefreshNeighbors(GrowBlock centerBlock)
        {
            if (centerBlock == null)
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

            if (block.CurrentBuildable is FencePost fencePost)
                fencePost.RefreshConnections(block);
        }

        private void TryRefreshFencePost(GrowBlock block, Vector2Int direction)
        {
            GrowBlock neighbor = GridManager.Instance.GetNeighbor(block, direction);

            if (neighbor == null)
                return;

            if (neighbor.CurrentBuildable is FencePost fencePost)
                fencePost.RefreshConnections(neighbor);
        }

        private bool HasFencePost(GrowBlock block, Vector2Int direction)
        {
            GrowBlock neighbor = GridManager.Instance.GetNeighbor(block, direction);

            if (neighbor == null)
                return false;

            return neighbor.CurrentBuildable is FencePost;
        }

        private void DestroyRandomActiveRail()
        {
            GameObject[] rails =
            {
                northRail,
                southRail,
                eastRail,
                westRail
            };

            int startIndex = Random.Range(0, rails.Length);

            for (int i = 0; i < rails.Length; i++)
            {
                int index = (startIndex + i) % rails.Length;

                if (rails[index] != null && rails[index].activeSelf)
                {
                    Destroy(rails[index]);
                    return;
                }
            }
        }

        private void DestroyDamagePiece(int index)
        {
            if (damagePieces == null)
                return;

            if (index < 0 || index >= damagePieces.Length)
                return;

            if (damagePieces[index] == null)
                return;

            Destroy(damagePieces[index]);
        }

        private void SetRailActive(GameObject rail, bool active)
        {
            if (rail == null)
                return;

            rail.SetActive(active);
        }
    }
}