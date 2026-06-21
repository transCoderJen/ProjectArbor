using System;
using System.Collections.Generic;
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
        private HashSet<AbstractCommandable> selectedUnits = new (12);

        private void Awake()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent += HandleUnitDeselected;
        }

        void Start()
        {
            actionsUI.Disable();
        }

        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeselectedEvent>.OnEvent -= HandleUnitDeselected;
        }

        private void HandleUnitSelected(UnitSelectedEvent evt)
        {
            if (evt.Unit is AbstractCommandable commandable)
            {
                selectedUnits.Add(commandable);
                actionsUI.EnableFor(selectedUnits);
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
                }
                else
                {
                    actionsUI.Disable();
                }
            }
        }
    }
}