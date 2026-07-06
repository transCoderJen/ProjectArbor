using System;
using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.SaveAndLoad;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class Worker : AbstractUnit, IBuildingBuilder
    {
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

        public BaseBuilding AssignedBuildTarget { get; private set; }

        public bool HasBuildAssignment =>
            AssignedBuildTarget != null &&
            AssignedBuildTarget.IsUnderConstruction;
        
        private UnitCommands commandBeforeBuild;
        private bool hasStoredCommandBeforeBuild;

        protected override void Start()
        {
            base.Start();

            if (graphAgent.GetVariable("GatherSuppliesEvent", out BlackboardVariable<GatherSuppliesEventChannel> eventChannelVariable))
            {
                eventChannelVariable.Value.Event += HandleGatherSupplies;
            }
        }

        public override void MoveTo(Vector3 position)
        {
            CancelCurrentJob();

            base.MoveTo(position);

        }

        public override void Stop()
        {
            CancelCurrentJob();

            base.Stop();
        }

        public void Gather()
        {
            CancelCurrentJob();

            graphAgent.SetVariableValue<GatherableSupply>("Supply", null);
            graphAgent.SetVariableValue<GameObject>("TargetGameObject", null);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }
        
        public void Gather(GatherableSupply supply)
        {
            CancelCurrentJob();

            graphAgent.SetVariableValue("Supply", supply);
            graphAgent.SetVariableValue("TargetGameObject", supply.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }

        public void ReturnSupplies(GameObject storehouse)
        {
            CancelCurrentJob();

            graphAgent.SetVariableValue("Storehouse", storehouse);
            graphAgent.SetVariableValue("Command", UnitCommands.ReturnSupplies);
        }

        public void Farm()
        {
            CancelCurrentJob();

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
            
            if (HasBuildAssignment)
                return;

            if (!hasStoredCommandBeforeBuild)
            {
                if (graphAgent.GetVariable("Command", out BlackboardVariable<UnitCommands> command))
                    commandBeforeBuild = command.Value;
                else
                    commandBeforeBuild = UnitCommands.Stop;
                
                hasStoredCommandBeforeBuild = true;
            }

            AssignedBuildTarget = building;

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

        public void ClearBuildAssignment()
        {
            AssignedBuildTarget = null;
        }

        private void CancelCurrentJob()
        {
            if (AssignedBuildTarget != null)
            {
                AssignedBuildTarget.ReleaseBuilder(this);
                AssignedBuildTarget = null;
            }

            commandBeforeBuild = UnitCommands.Stop;
            hasStoredCommandBeforeBuild = false;
        }

        public void ResumePreviousCommand()
        {
            UnitCommands commandToResume = commandBeforeBuild;

            commandBeforeBuild = UnitCommands.Stop;
            hasStoredCommandBeforeBuild = false;

            if (!ShouldResumeAfterBuild(commandToResume))
            {
                graphAgent.SetVariableValue("Command", UnitCommands.Stop);
                return;
            }

            graphAgent.SetVariableValue("Command", commandToResume);
        }

        private bool ShouldResumeAfterBuild(UnitCommands command)
        {
            return command switch
            {
                UnitCommands.Farm => true,
                UnitCommands.Gather => true,
                UnitCommands.ReturnSupplies => true,
                _ => false
            };
        }

        public void FinishBuildAssignment()
        {
            AssignedBuildTarget = null;

            if (UnitManager.Instance != null &&
                UnitManager.Instance.TryAssignWorkerToNextConstructionSite(this))
            {
                return;
            }

            ResumePreviousCommand();
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