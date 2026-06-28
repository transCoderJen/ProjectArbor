using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Gather Command", menuName = "Units/Commands/Gather", order = 105)]
    public class GatherCommand : BaseCommand
    {
        [SerializeField] private BuildingSO storehouseSO;
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is Worker
                && context.Hit.collider != null
                && IsGatherableSupplyOrStorehouse(context.Hit.collider);
        }

        public override void Handle(CommandContext context)
        {
            Worker worker = context.Commandable as Worker;
            if (context.Hit.collider.TryGetComponent(out GatherableSupply supply))
            {
                worker.Gather(supply); 
            }
            else if (IsStorehouse(context.Hit.collider) && worker.HasSupplies)
            {
                worker.ReturnSupplies(context.Hit.collider.gameObject);
            }
            else
            {
                worker.MoveTo(context.Hit.collider.gameObject.transform.position);
            }
        }

        private bool IsGatherableSupplyOrStorehouse(Collider collider) => collider.TryGetComponent(out GatherableSupply _) || IsStorehouse(collider);
        private bool IsStorehouse(Collider collider) => collider.TryGetComponent(out BaseBuilding building) && building.UnitSO.Equals(storehouseSO);
    }
}