using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private BuildingBuildingUI buildingBuildingUI;
        private HashSet<AbstractCommandable> selectedUnits = new (12);

        private void Awake()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
            Bus<SupplyEvent>.OnEvent += HandleSupplyChange;
        }

        void Start()
        {
            actionsUI.Disable();
            buildingBuildingUI.Disable();
        }

        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
            Bus<SupplyEvent>.OnEvent -= HandleSupplyChange;
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

                if (selectedUnits.Count == 1 && selectedUnits.First() is BaseBuilding building)
                {
                    buildingBuildingUI.EnableFor(building);
                }
                else
                {
                    buildingBuildingUI.Disable();
                }
            }
            else
            {
                actionsUI.Disable();
                buildingBuildingUI.Disable();
            }
        }

        private void HandleUnitSelected(UnitSelectedEvent evt)
        {
            if (evt.Unit is AbstractCommandable commandable)
            {
                selectedUnits.Add(commandable);
                actionsUI.EnableFor(selectedUnits);
            }

            if (selectedUnits.Count == 1 && evt.Unit is BaseBuilding building)
            {
                buildingBuildingUI.EnableFor(building);
            }
        }

        private void HandleUnitDeselected(UnitDeselectedEvent evt)
        {
            if (evt.Unit is AbstractCommandable commandable)
            {
                selectedUnits.Remove(commandable);

                if (selectedUnits.Count > 0)
                {
                    actionsUI.EnableFor(selectedUnits);
                    
                    if (selectedUnits.Count == 1 && selectedUnits.First() is BaseBuilding building)
                    {
                        buildingBuildingUI.EnableFor(building);
                    }
                    else
                    {
                        buildingBuildingUI.Disable();
                    }
                }
                else
                {
                    actionsUI.Disable();
                    buildingBuildingUI.Disable();
                }
            }
        }

        private void HandleSupplyChange(SupplyEvent evt)
        {
            actionsUI.EnableFor(selectedUnits);
        }
    }
}