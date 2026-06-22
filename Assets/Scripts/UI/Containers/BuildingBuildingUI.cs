using System;
using System.Collections;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;


namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class BuildingBuildingUI : MonoBehaviour, IUIElement<BaseBuilding>
    {
        [SerializeField] private UIBuildQueueButton[] unitButtons;
        [SerializeField] private ProgressBar progressBar;

        private Coroutine buildCoroutine;
        private BaseBuilding building;
        
        public void EnableFor(BaseBuilding item)
        {
            progressBar.SetProgress(0f);
            gameObject.SetActive(true);
            building = item;
            building.OnQueueUpdated += HandleQueueUpdated;
            SetupUnitButtons();

            buildCoroutine = StartCoroutine(UpdateUnitProgress());
        }

        private void SetupUnitButtons()
        {
            int i = 0;
            for (; i < building.QueueSize; i++)
            {
                int index = i;
                unitButtons[i].EnableFor(building.Queue[i], () => building.CancelBuildingUnit(index));
            }

            for (; i < unitButtons.Length; i++)
            {
                unitButtons[i].Disable();
            }
        }

        public void Disable()
        {
            if (building != null)
            {
                building.OnQueueUpdated -= HandleQueueUpdated;
            }

            gameObject.SetActive(false);
            building = null;
            buildCoroutine = null;
        }

        private void HandleQueueUpdated(AbstractUnitSO[] unitsInQueue)
        {
            if (unitsInQueue.Length == 1 && buildCoroutine == null)
            {
                buildCoroutine = StartCoroutine(UpdateUnitProgress());
            }

            SetupUnitButtons();
        }

        private IEnumerator UpdateUnitProgress()
        {
            while (building != null && building.QueueSize > 0)
            {
                float startTime = building.CurrentQueueStartTime;
                float endTime = startTime + building.BuildingUnit.BuildTime;

                float progress = Mathf.Clamp01((Time.time -startTime) / (endTime - startTime));
                progressBar.SetProgress(progress);
                yield return null;
            }

            
            buildCoroutine = null;
        }
    }
}