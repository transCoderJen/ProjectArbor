using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.SaveAndLoad;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class Worker : AbstractUnit, IBuildingBuilder
    {
        public override CombatTeam Team => CombatTeam.Friendly;

        public bool HasSupplies
        {
            get
            {
                if (graphAgent != null &&
                    graphAgent.GetVariable("SupplyAmountHeld", out BlackboardVariable<int> heldVariable))
                {
                    return heldVariable.Value > 0;
                }

                return false;
            }
        }

        public bool HasWater
        {
            get
            {
                if (graphAgent != null &&
                    graphAgent.GetVariable("WaterAmountHeld", out BlackboardVariable<int> heldVariable))
                {
                    return heldVariable.Value > 0;
                }

                return false;
            }
        }

        public bool HasFertilizer
        {
            get
            {
                if (graphAgent != null &&
                    graphAgent.GetVariable("FertilizerAmountHeld", out BlackboardVariable<int> heldVariable))
                {
                    return heldVariable.Value > 0;
                }

                return false;
            }
        }

        public bool IsBuilding => throw new System.NotImplementedException();

        protected override void Start()
        {
            base.Start();

            if (graphAgent.GetVariable("GatherSuppliesEvent", out BlackboardVariable<GatherSuppliesEventChannel> eventChannelVariable))
            {
                eventChannelVariable.Value.Event += HandleGatherSupplies;
            }
        }

        public void Gather()
        {
            graphAgent.SetVariableValue<GatherableSupply>("Supply", null);
            graphAgent.SetVariableValue<GameObject>("TargetGameObject", null);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }
        
        public void Gather(GatherableSupply supply)
        {
            graphAgent.SetVariableValue("Supply", supply);
            graphAgent.SetVariableValue("TargetGameObject", supply.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }

        public void ReturnSupplies(GameObject storehouse)
        {
            graphAgent.SetVariableValue("Storehouse", storehouse);
            graphAgent.SetVariableValue("Command", UnitCommands.ReturnSupplies);
        }

        public void Farm()
        {
            graphAgent.SetVariableValue<GameObject>("FarmTarget", null);
            graphAgent.SetVariableValue<GameObject>("FarmSource", null);
            graphAgent.SetVariableValue<FarmTaskType>("FarmTask", FarmTaskType.None);

            graphAgent.SetVariableValue("Command", UnitCommands.Farm);
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

#region Buiild
        public void Build(BaseBuilding building)
        {
            if (building == null)
                return;

            graphAgent.SetVariableValue("BuildTarget", building);
            graphAgent.SetVariableValue("TargetGameObject", building.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Build);
        }

        public void ResumeBuilding(BaseBuilding building)
        {
            throw new System.NotImplementedException();
        }

        public void CancelBuilding()
        {
            throw new System.NotImplementedException();
        }
#endregion

#region Load/ Save
        public override void WriteToSaveData(UnitSaveData data)
        {
            base.WriteToSaveData(data);

            data.WaterAmountHeld = GetBlackboardInt("WaterAmountHeld");
            data.FertilizerAmountHeld = GetBlackboardInt("FertilizerAmountHeld");
            data.SeedAmountHeld = GetBlackboardInt("SeedAmountHeld");

            ItemData_Seed seedHeld = GetBlackboardSeed("SeedHeld");
            data.SeedHeldID = seedHeld != null ? seedHeld.ItemID : string.Empty;
        }

        public override void RestoreFromSave(UnitSaveData data)
        {
            base.RestoreFromSave(data);

            graphAgent.SetVariableValue("WaterAmountHeld", data.WaterAmountHeld);
            graphAgent.SetVariableValue("FertilizerAmountHeld", data.FertilizerAmountHeld);
            graphAgent.SetVariableValue("SeedAmountHeld", data.SeedAmountHeld);

            ItemData_Seed seed = GetSeedByID(data.SeedHeldID);
            graphAgent.SetVariableValue("SeedHeld", seed);
        }

        private int GetBlackboardInt(string variableName)
        {
            if (graphAgent.GetVariable(variableName, out BlackboardVariable<int> variable))
                return variable.Value;

            return 0;
        }
        
        private ItemData_Seed GetBlackboardSeed(string variableName)
        {
            if (graphAgent.GetVariable(variableName, out BlackboardVariable<ItemData_Seed> variable))
                return variable.Value;

            return null;
        }

        private ItemData_Seed GetSeedByID(string seedID)
        {
            if (string.IsNullOrEmpty(seedID))
                return null;

            if (Inventory.Instance == null)
                return null;

            foreach (ItemData item in Inventory.Instance.itemDataBase)
            {
                if (item == null)
                    continue;

                if (item.ItemID != seedID)
                    continue;

                return item as ItemData_Seed;
            }

            return null;
        }
#endregion
    }
}