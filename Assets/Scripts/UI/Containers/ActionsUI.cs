using System;
using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class ActionsUI : MonoBehaviour, IUIElement<HashSet<AbstractCommandable>>
    {
        [SerializeField] private UIActionButton[] actionButtons;

        public void EnableFor(HashSet<AbstractCommandable> selectedUnits)
        {
            RefreshButtons(selectedUnits);
        }

        public void Disable()
        {
            foreach (UIActionButton actionButton in actionButtons)
            {
                actionButton.Disable();
            }
        }
        
        private void RefreshButtons(HashSet<AbstractCommandable> selectedUnits)
        {
            HashSet<BaseCommand> availableCommands = new(9);
            
            foreach (AbstractCommandable commandable in selectedUnits)
            {
                availableCommands.UnionWith(commandable.AvailableCommands);
            }

            for (int i = 0; i < actionButtons.Length; i++)
            {
                BaseCommand actionForSlot = availableCommands.Where(action => action.Slot == i).FirstOrDefault();

                if (actionForSlot != null)
                {
                    actionButtons[i].EnableFor(actionForSlot, HandleClick(actionForSlot));
                }
                else
                {
                    actionButtons[i].Disable();
                }
            }
        }

        private UnityAction HandleClick(BaseCommand action)
        {
            return () => Bus<ActionSelectedEvent>.Raise(new ActionSelectedEvent(action));
        }
    }
}
