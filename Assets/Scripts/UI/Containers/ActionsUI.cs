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

            availableCommands = availableCommands.Where(action => action.IsAvailable(
                new CommandContext(
                    selectedUnits.FirstOrDefault(), new RaycastHit()
                )
            )).ToHashSet();

            for (int i = 0; i < actionButtons.Length; i++)
            {
                BaseCommand commandForSlot = availableCommands.FirstOrDefault(command => command.Slot == i);

                if (commandForSlot != null)
                {
                    actionButtons[i].EnableFor(commandForSlot, HandleClick(commandForSlot, selectedUnits.First()));
                }
                else
                {
                    actionButtons[i].Disable();
                }
            }
        }

        private UnityAction HandleClick(BaseCommand action, AbstractCommandable selectedUnit)
        {
            return () =>
            {
                action.Activate(selectedUnit);

                Bus<ActionSelectedEvent>.Raise(new ActionSelectedEvent(action));
            };
        }
    }
}
