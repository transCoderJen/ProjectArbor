using System.Linq;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.UserInterface.Components;
using UnityEngine;
using UnityEngine.Events;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class ConstructionMenuUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private GameObject menuRoot;

        [Header("Commands")]
        [SerializeField] private BuildBuildingCommand[] buildCommands;
        [SerializeField] private UIActionButton[] actionButtons;

        public bool IsOpen =>
            menuRoot != null &&
            menuRoot.activeSelf;

        private void Awake()
        {
            Close();
        }

        public void Open()
        {
            if (menuRoot == null)
                return;

            menuRoot.SetActive(true);
            RefreshButtons();
        }

        public void Close()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            DisableButtons();
        }

        private void RefreshButtons()
        {
            for (int i = 0; i < actionButtons.Length; i++)
            {
                BuildBuildingCommand commandForSlot =
                    buildCommands.FirstOrDefault(
                        command =>
                            command != null &&
                            command.Slot == i);

                if (commandForSlot == null)
                {
                    actionButtons[i].Disable();
                    continue;
                }

                actionButtons[i].EnableFor(
                    commandForSlot,
                    HandleClick(commandForSlot));
            }
        }

        private UnityAction HandleClick(
            BuildBuildingCommand command)
        {
            return () =>
            {
                command.ActivatePlacement();

                Bus<ActionSelectedEvent>.Raise(
                    new ActionSelectedEvent(command));

                Close();
            };
        }

        private void DisableButtons()
        {
            foreach (UIActionButton actionButton in actionButtons)
            {
                if (actionButton != null)
                    actionButton.Disable();
            }
        }
    }
}