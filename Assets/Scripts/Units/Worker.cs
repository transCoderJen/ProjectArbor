using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class Worker : AbstractUnit
    {
        public override CombatTeam Team => CombatTeam.Friendly;

        public bool HasSupplies
        {
            get
            {
                if (graphAgent!= null && graphAgent.GetVariable("SupplyAmountHeld", out BlackboardVariable<int> heldVariable))
                {
                    return heldVariable.Value > 0;
                }

                return false;
            }
        }

        public void ReturnSupplies(GameObject storehouse)
        {
            graphAgent.SetVariableValue("Storehouse", storehouse);
            graphAgent.SetVariableValue("Command", UnitCommands.ReturnSupplies);
        }

        protected override void Start()
        {
            base.Start();
            if (graphAgent.GetVariable("GatherSuppliesEvent", out BlackboardVariable<GatherSuppliesEventChannel> eventChannelVariable))
            {
                eventChannelVariable.Value.Event += HandleGatherSupplies;
            }
        }

        public void Gather(GatherableSupply supply)
        {
            graphAgent.SetVariableValue("Supply", supply);
            graphAgent.SetVariableValue("TargetGameObject", supply.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }

        private void HandleGatherSupplies(GameObject self, int amount, SupplySO supply)
        {
            if (self != gameObject)
                return;

            if (supply == null)
            {
                Debug.LogError($"{name} gathered supplies, but SupplySO was null.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning($"{name} gathered invalid amount: {amount}");
                return;
            }

            Bus<SupplyEvent>.Raise(new SupplyEvent(amount, supply));
        }
    }
}

