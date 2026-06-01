using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Buildable
{
    public class BaseBuildable : MonoBehaviour
    {
        [field: SerializeField] public MeshRenderer MainRenderer { get; private set; }
        [SerializeField] private Material PrimaryMaterial;
        [SerializeField] private LayerMask GridLayer;
        [SerializeField] private BuildableData buildableData;

        public void Build()
        {
            MainRenderer.material = PrimaryMaterial;
        }

        public bool AllRestrictionsPass()
        {
            Ray ray = Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (!Physics.Raycast(ray, out RaycastHit hit, math.INFINITY, GridLayer))
            {
                Debug.Log("GridLayer Not found");
                return false;
            }

            if (!hit.collider.TryGetComponent<GrowBlock>(out GrowBlock growBlock))
            {
                Debug.Log("GridBlock Not Found");
                return false;
            }

            if (!buildableData.CanAfford())
            {
                return false;
            }

            return growBlock.IsActive;
        }
    }
}