using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Containers;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Managers
{
    public class RuntimeUI : MonoBehaviour
    {
        [SerializeField] private ActionsUI actionsUI;
        // [SerializeField] private BuildingBuildingUI buildingBuildingUI;
        [SerializeField] private BuildingSelectedUI buildingSelectedUI;

        [SerializeField] private UnitIconUI unitIconUI;
        [SerializeField] private SingleUnitSelectedUI singleUnitSelectedUI;

        private HashSet<AbstractCommandable> selectedUnits = new (12);

        private void Awake()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
            Bus<SupplyEvent>.OnEvent += HandleSupplyChange;
            Bus<UpgradeResearchEvent>.OnEvent += HandleUpgradeResearch;
            Bus<BuildingSpawnEvent>.OnEvent += HandleBuildingSpawn;
        }

        void Start()
        {
            actionsUI.Disable();
            buildingSelectedUI.Disable();
            unitIconUI.Disable();
        }

        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
            Bus<SupplyEvent>.OnEvent -= HandleSupplyChange;
            Bus<UpgradeResearchEvent>.OnEvent -= HandleUpgradeResearch;
            Bus<BuildingSpawnEvent>.OnEvent -= HandleBuildingSpawn;
        }

        private void HandleBuildingSpawn(BuildingSpawnEvent _)
        {
            if (selectedUnits.Count == 1 && selectedUnits.First() is Worker)
            {
                actionsUI.EnableFor(selectedUnits);
            }
        }

        private void HandleUpgradeResearch(UpgradeResearchEvent _)
        {
            RefreshUI();
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            selectedUnits.Remove(evt.Unit);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (selectedUnits.Count > 0)
            {
                actionsUI.EnableFor(selectedUnits);

                if (selectedUnits.Count == 1)
                {
                    AbstractCommandable commandable = selectedUnits.First();
                    unitIconUI.EnableFor(selectedUnits.First());
                    singleUnitSelectedUI.EnableFor(commandable);

                    if (commandable is BaseBuilding building)
                    {
                        singleUnitSelectedUI.Disable();
                        buildingSelectedUI.EnableFor(building);
                    }
                    else
                    {
                        buildingSelectedUI.Disable();
                        singleUnitSelectedUI.EnableFor(commandable);
                    }
                }
                else
                {
                    unitIconUI.Disable();
                    singleUnitSelectedUI.Disable();
                    buildingSelectedUI.Disable();
                }
            }
            else
            {
                actionsUI.Disable();
                buildingSelectedUI.Disable();
                unitIconUI.Disable();
                singleUnitSelectedUI.Disable();
            }
        }

        private void HandleUnitSelected(UnitSelectedEvent evt)
        {
            if (evt.Unit is AbstractCommandable commandable)
            {
                selectedUnits.Add(commandable);
                // actionsUI.EnableFor(selectedUnits);
                RefreshUI();
            }

            // if (selectedUnits.Count == 1 && evt.Unit is BaseBuilding building)
            // {
            //     buildingBuildingUI.EnableFor(building);
            // }
        }

        private void HandleUnitDeselected(UnitDeselectedEvent evt)
        {
            if (evt.Unit is AbstractCommandable commandable)
            {
                selectedUnits.Remove(commandable);

                RefreshUI();
            }
        }

        // private void HandleUnitDeselected(UnitDeselectedEvent evt)
        // {
        //     if (evt.Unit is AbstractCommandable commandable)
        //     {
        //         selectedUnits.Remove(commandable);

        //         if (selectedUnits.Count > 0)
        //         {
        //             actionsUI.EnableFor(selectedUnits);
                    
        //             if (selectedUnits.Count == 1 && selectedUnits.First() is BaseBuilding building)
        //             {
        //                 buildingBuildingUI.EnableFor(building);
        //             }
        //             else
        //             {
        //                 buildingBuildingUI.Disable();
        //             }
        //         }
        //         else
        //         {
        //             actionsUI.Disable();
        //             buildingBuildingUI.Disable();
        //             unitIconUI.Disable();
        //         }
        //     }
        // }

        private void HandleSupplyChange(SupplyEvent evt)
        {
            actionsUI.EnableFor(selectedUnits);
        }
    }
}