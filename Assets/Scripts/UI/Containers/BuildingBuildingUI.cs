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
            Disable();

            building = item;

            if (building == null)
                return;

            gameObject.SetActive(true);

            if (progressBar != null)
                progressBar.SetProgress(0f);

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

            if (progressBar != null)
                progressBar.SetProgress(0f);

            if (unitButtons != null)
            {
                foreach (UIBuildQueueButton button in unitButtons)
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

        private void HandleQueueUpdated(AbstractUnitSO[] unitsInQueue)
        {
            if (building == null)
                return;

            SetupUnitButtons();
            StartProgressRoutineIfNeeded();
        }

        private void SetupUnitButtons()
        {
            if (building == null || unitButtons == null)
                return;

            AbstractUnitSO[] queue = building.Queue;

            int buttonCount = unitButtons.Length;
            int queueCount = Mathf.Min(queue.Length, buttonCount);

            for (int i = 0; i < queueCount; i++)
            {
                int index = i;

                if (unitButtons[i] == null)
                    continue;

                unitButtons[i].EnableFor(
                    queue[i],
                    () =>
                    {
                        if (building != null)
                            building.CancelBuildingUnit(index);
                    });
            }

            for (int i = queueCount; i < buttonCount; i++)
            {
                if (unitButtons[i] != null)
                    unitButtons[i].Disable();
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
                AbstractUnitSO currentUnit = building.BuildingUnit;

                if (currentUnit == null || progressBar == null)
                {
                    yield return null;
                    continue;
                }

                float startTime = building.CurrentQueueStartTime;
                float buildTime = Mathf.Max(0.01f, currentUnit.BuildTime);
                float progress = Mathf.Clamp01((Time.time - startTime) / buildTime);

                progressBar.SetProgress(progress);

                yield return null;
            }

            if (progressBar != null)
                progressBar.SetProgress(0f);

            buildCoroutine = null;
        }

        public void SetBuildProgress(float current, float required)
        {
            if (progressBar == null)
                return;

            float progress = required <= 0f
                ? 1f
                : Mathf.Clamp01(current / required);

            progressBar.SetProgress(progress);
        }
    }
}