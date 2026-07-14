using System.Collections;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.UserInterface.Managers;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class BuildingBuildingUI : MonoBehaviour, IUIElement<BaseBuilding>
    {
        [SerializeField] private TextMeshProUGUI BuildingText;
        [SerializeField] private UIBuildQueueButton[] UnitButtons;
        [SerializeField] private ProgressBar ProgressBar;

        private Coroutine buildCoroutine;
        private BaseBuilding building;

        public void EnableFor(BaseBuilding item)
        {
            Disable();

            building = item;

            if (building == null)
                return;

            gameObject.SetActive(true);

            if (ProgressBar != null)
                ProgressBar.SetProgress(0f);

            building.OnQueueUpdated += HandleQueueUpdated;

            SetupUnitButtons();
            StartProgressRoutineIfNeeded();
        }

        public void Disable()
        {
            if (building != null)
                building.OnQueueUpdated -= HandleQueueUpdated;

            if (buildCoroutine != null)
            {
                StopCoroutine(buildCoroutine);
                buildCoroutine = null;
            }

            building = null;

            if (ProgressBar != null)
                ProgressBar.SetProgress(0f);

            if (UnitButtons != null)
            {
                foreach (UIBuildQueueButton button in UnitButtons)
                {
                    if (button != null)
                        button.Disable();
                }
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (building != null)
                building.OnQueueUpdated -= HandleQueueUpdated;

            if (buildCoroutine != null)
            {
                StopCoroutine(buildCoroutine);
                buildCoroutine = null;
            }

            building = null;
        }

        private void HandleQueueUpdated(UnlockableSO[] unitsInQueue)
        {
            if (building == null)
                return;

            SetupUnitButtons();
            StartProgressRoutineIfNeeded();
        }

        private void SetupUnitButtons()
        {
            if (building == null || UnitButtons == null)
                return;

            UnlockableSO[] queue = building.Queue;

            int buttonCount = UnitButtons.Length;
            int queueCount = Mathf.Min(queue.Length, buttonCount);

            for (int i = 0; i < queueCount; i++)
            {
                int index = i;

                if (UnitButtons[i] == null)
                    continue;

                UnitButtons[i].EnableFor(
                    queue[i],
                    () =>
                    {
                        if (building != null)
                            building.CancelBuildingUnit(index);
                    });
            }

            for (int i = queueCount; i < buttonCount; i++)
            {
                if (UnitButtons[i] != null)
                    UnitButtons[i].Disable();
            }
        }

        private void StartProgressRoutineIfNeeded()
        {
            if (building == null)
                return;

            if (building.QueueSize <= 0)
                return;

            if (buildCoroutine != null)
                return;

            buildCoroutine = StartCoroutine(UpdateUnitProgress());
        }

        private IEnumerator UpdateUnitProgress()
        {
            while (building != null && building.QueueSize > 0)
            {
                UnlockableSO currentUnit = building.SOBeingBuilt;

                if (currentUnit == null || ProgressBar == null)
                {
                    yield return null;
                    continue;
                }

                float startTime = building.CurrentQueueStartTime;
                float buildTime = Mathf.Max(0.01f, currentUnit.BuildTime);
                float progress = Mathf.Clamp01((Time.time - startTime) / buildTime);

                ProgressBar.SetProgress(progress);

                yield return null;
            }

            if (ProgressBar != null)
                ProgressBar.SetProgress(0f);

            buildCoroutine = null;
        }

        public void SetBuildProgress(float current, float required)
        {
            if (ProgressBar == null)
                return;

            float progress = required <= 0f
                ? 1f
                : Mathf.Clamp01(current / required);

            ProgressBar.SetProgress(progress);
        }
    }
}