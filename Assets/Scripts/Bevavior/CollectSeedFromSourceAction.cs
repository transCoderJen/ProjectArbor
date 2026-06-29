using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Behavior
{    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Collect Seed from Source", story: "[Unit] collects seed from [SeedSource]", category: "Action/Units", id: "9b25a30656d14ec1fad7bd776378b5ec")]
    public partial class CollectSeedFromSourceAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> SeedSource;

        [SerializeReference] public BlackboardVariable<ItemData_Seed> SeedHeld;
        [SerializeReference] public BlackboardVariable<int> SeedAmountHeld;
        [SerializeReference] public BlackboardVariable<int> SeedCapacity;

        private ISeedSource source;

        protected override Status OnStart()
        {
            Debug.Log("CollectSeedFromSourceAction started.");

            if (Unit.Value == null)
            {
                Debug.LogWarning("CollectSeed failed: Unit is null.");
                return Status.Failure;
            }

            if (SeedSource.Value == null)
            {
                Debug.LogWarning("CollectSeed failed: SeedSource is null.");
                return Status.Failure;
            }

            Debug.Log($"CollectSeed Unit={Unit.Value.name}, SeedSource={SeedSource.Value.name}");

            if (!Unit.Value.TryGetComponent(out Worker worker))
            {
                Debug.LogWarning($"CollectSeed failed: {Unit.Value.name} has no Worker component.");
                return Status.Failure;
            }

            source = SeedSource.Value.GetComponentInParent<ISeedSource>();

            if (source == null)
            {
                Debug.Log("CollectSeed: No ISeedSource in parent. Checking children.");
                source = SeedSource.Value.GetComponentInChildren<ISeedSource>();
            }

            if (source == null)
            {
                Debug.LogWarning($"CollectSeed failed: {SeedSource.Value.name} has no ISeedSource.");
                return Status.Failure;
            }

            Debug.Log($"CollectSeed found source: {((MonoBehaviour)source).name}");

            int requestedAmount = SeedCapacity.Value - SeedAmountHeld.Value;

            Debug.Log(
                $"CollectSeed inventory check | " +
                $"SeedCapacity={SeedCapacity.Value}, " +
                $"SeedAmountHeld={SeedAmountHeld.Value}, " +
                $"RequestedAmount={requestedAmount}");

            if (requestedAmount <= 0)
            {
                Debug.LogWarning("CollectSeed failed: RequestedAmount <= 0.");
                return Status.Failure;
            }

            ItemData_Seed seed = source.GetNextSeed();

            if (seed == null)
            {
                Debug.LogWarning("CollectSeed failed: GetNextSeed returned null.");
                return Status.Failure;
            }

            Debug.Log($"CollectSeed selected seed: {seed.name}");

            bool canProvide = source.CanProvideSeed(seed, requestedAmount);

            Debug.Log(
                $"CollectSeed CanProvideSeed check | " +
                $"Seed={seed.name}, RequestedAmount={requestedAmount}, CanProvide={canProvide}");

            if (!canProvide)
            {
                Debug.LogWarning("CollectSeed failed: Source cannot provide requested amount.");
                return Status.Failure;
            }

            bool reserved = source.TryBeginCollectSeed(worker);

            Debug.Log($"CollectSeed reservation result: {reserved}");

            if (!reserved)
            {
                Debug.LogWarning("CollectSeed failed: Could not reserve source.");
                return Status.Failure;
            }

            bool collected =
                source.TryCompleteCollectSeed(worker, seed, requestedAmount);

            Debug.Log($"CollectSeed completion result: {collected}");

            if (!collected)
            {
                Debug.LogWarning("CollectSeed failed: TryCompleteCollectSeed returned false. Aborting reservation.");
                source.AbortCollectSeed(worker);
                return Status.Failure;
            }

            SeedHeld.Value = seed;
            SeedAmountHeld.Value += requestedAmount;
            SeedSource.Value = null;

            Debug.Log(
                $"CollectSeed success | " +
                $"SeedHeld={SeedHeld.Value.name}, " +
                $"SeedAmountHeld={SeedAmountHeld.Value}");

            return Status.Success;
        }
    }
}