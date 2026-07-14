using NUnit.Framework;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class BuildingSelectedUI : MonoBehaviour, IUIElement<BaseBuilding>
    {
        [SerializeField] private SingleUnitSelectedUI singleUnitSelectedUI;
        [SerializeField] private BuildingBuildingUI buildingBuildingUI;
        [SerializeField] private BuildingUnderConstructionUI buildingUnderConstructionUI;

        private BaseBuilding selectedBuilding;

        public void EnableFor(BaseBuilding building)
        {
            UnsubscribeFromSelectedBuilding();

            selectedBuilding = building;

            if (selectedBuilding == null)
            {
                Disable();
                return;
            }

            gameObject.SetActive(true);

            selectedBuilding.OnQueueUpdated += OnBuildingQueueUpdated;
            selectedBuilding.OnBuildCompleted += HandleBuildCompleted;

            RefreshUI();
        }

        public void Disable()
        {
            UnsubscribeFromSelectedBuilding();

            buildingBuildingUI.Disable();
            singleUnitSelectedUI.Disable();
            buildingUnderConstructionUI.Disable();

            gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            if (selectedBuilding == null)
                return;

            if (selectedBuilding.IsUnderConstruction)
            {
                buildingBuildingUI.Disable();
                singleUnitSelectedUI.Disable();

                buildingUnderConstructionUI.EnableFor(selectedBuilding);
                return;
            }

            buildingUnderConstructionUI.Disable();
            OnBuildingQueueUpdated();
        }

        private void HandleBuildCompleted()
        {
            if (selectedBuilding == null)
                return;

            buildingUnderConstructionUI.Disable();
            OnBuildingQueueUpdated();
        }

        private void OnBuildingQueueUpdated(UnlockableSO[] _ = null)
        {
            if (selectedBuilding == null)
                return;

            if (selectedBuilding.QueueSize == 0)
            {
                buildingBuildingUI.Disable();
                singleUnitSelectedUI.EnableFor(selectedBuilding);
            }
            else
            {
                singleUnitSelectedUI.Disable();

                // The parent containing BuildingBuildingUI must be active first.
                gameObject.SetActive(true);

                buildingBuildingUI.EnableFor(selectedBuilding);
            }
        }

        private void UnsubscribeFromSelectedBuilding()
        {
            if (selectedBuilding == null)
                return;

            selectedBuilding.OnQueueUpdated -= OnBuildingQueueUpdated;
            selectedBuilding.OnBuildCompleted -= HandleBuildCompleted;

            selectedBuilding = null;
        }

        private void OnDisable()
        {
            UnsubscribeFromSelectedBuilding();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSelectedBuilding();
        }
    }
}