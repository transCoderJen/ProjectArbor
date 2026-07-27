using System.Linq;
using ShiftedSignal.Garden.Buildable;
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

        [Header("Placement")]
        [Tooltip(
            "Place this outside menuRoot so it remains visible during placement.")]
        [SerializeField] private GameObject placementBackHint;

        [Header("Commands")]
        [SerializeField] private BuildBuildingCommand[] buildCommands;
        [SerializeField] private UIActionButton[] actionButtons;

        private BuildingSO selectedBuilding;
        private BuildBuildingCommand selectedCommand;

        public bool IsOpen =>
            menuRoot != null &&
            menuRoot.activeSelf;

        public bool IsPlacingBuilding { get; private set; }

        private void Awake()
        {
            HideMenu();
            SetPlacementHint(false);
        }

        public void Open()
        {
            if (menuRoot == null)
                return;

            // Every opening starts a completely new selection session.
            selectedCommand = null;
            selectedBuilding = null;
            IsPlacingBuilding = false;

            SetPlacementHint(false);
            menuRoot.SetActive(true);

            RefreshButtons();
        }

        /// <summary>
        /// Called by the construction menu's Close button.
        /// Ends construction mode and resumes the paused dialogue.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
                return;

            IsPlacingBuilding = false;
            selectedCommand = null;
            selectedBuilding = null;

            HideMenu();
            SetPlacementHint(false);

            Bus<ConstructionMenuClosedEvent>.Raise(
                new ConstructionMenuClosedEvent(null));
        }

        /// <summary>
        /// Called after Escape cancels active building placement.
        /// Reopens the menu without resuming dialogue.
        /// </summary>
        public void ReturnFromPlacement()
        {
            if (!IsPlacingBuilding)
                return;

            IsPlacingBuilding = false;

            SetPlacementHint(false);

            if (menuRoot != null)
                menuRoot.SetActive(true);

            RefreshButtons();
        }

        private void RefreshButtons()
        {
            for (int i = 0; i < actionButtons.Length; i++)
            {
                UIActionButton actionButton = actionButtons[i];

                if (actionButton == null)
                    continue;

                BuildBuildingCommand commandForSlot =
                    buildCommands.FirstOrDefault(
                        command =>
                            command != null &&
                            command.Slot == i);

                if (commandForSlot == null)
                {
                    actionButton.Disable();
                    continue;
                }

                actionButton.EnableFor(
                    commandForSlot,
                    HandleClick(commandForSlot));
            }
        }

        private UnityAction HandleClick(BuildBuildingCommand command)
        {
            return () =>
            {
                Debug.Log("Command is " + command);
                if (command == null)
                    return;

                selectedCommand = command;
                selectedBuilding = command.Building;

                HideMenu();

                Bus<ConstructionMenuClosedEvent>.Raise(
                    new ConstructionMenuClosedEvent(selectedBuilding));
            };
        }

        public void BeginSelectedBuildingPlacement()
        {
            Debug.Log(
        "[ConstructionMenuUI] BeginSelectedBuildingPlacement\n" +
        $"  selectedCommand: {(selectedCommand != null ? selectedCommand.name : "<null>")}\n" +
        $"  selectedBuilding: {(selectedBuilding != null ? selectedBuilding.ItemID : "<null>")}\n" +
        $"  menu instance ID: {GetInstanceID()}\n" +
        $"  frame: {Time.frameCount}");

            if (selectedCommand == null)
            {
                Debug.LogError(
                    "Cannot begin placement because selectedCommand is null.");
                return;
            }

            BuildBuildingCommand commandToActivate = selectedCommand;

            // Clear the pending selection before activation so it cannot
            // accidentally be reused later.
            selectedCommand = null;
            selectedBuilding = null;

            IsPlacingBuilding = true;

            commandToActivate.ActivatePlacement();

            SetPlacementHint(true);
        }

        private void HideMenu()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            DisableButtons();
        }

        private void SetPlacementHint(bool visible)
        {
            if (placementBackHint != null)
                placementBackHint.SetActive(visible);
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