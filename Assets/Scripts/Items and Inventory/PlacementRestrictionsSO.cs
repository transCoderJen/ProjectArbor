using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    [CreateAssetMenu(fileName = "Planting Restriction", menuName = "Plants/Restrictions", order = 7)]
    public class PlacementRestrictionsSO : ScriptableObject
    {
        [field: SerializeField] public int Range { get; private set; }
        [field: SerializeField] public ItemData_Seed Seed { get; private set; }

        public bool CanPlace(int GridX, int GridY)
        {
            for (int x = GridX - Range; x <= GridX + Range; x++)
            {
                for (int y = GridY - Range; y <= GridY + Range; y++)
                {

                    GrowBlock block = GridManager.Instance.GetBlock(x, y);

                    if (block == null || block == this)
                        continue;

                    Debug.Log(block.Seed);
                    if (block.Seed == Seed)
                        return false;
                }
            }

            return true;
        }
    }
    
}
