using System;
using System.Collections;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public class Supplies : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI fiberText;
        [SerializeField] private TextMeshProUGUI populationText;

        [Header("Starting Values")]
        [SerializeField] private int startingWood;
        [SerializeField] private int startingStone;
        [SerializeField] private int startingFiber;
        [SerializeField] private int startingPopulation;
        [SerializeField] private int startingPopulationLimit;

        [Header("Supply Types")]
        [SerializeField] private SupplySO woodSO;
        [SerializeField] private SupplySO stoneSO;
        [SerializeField] private SupplySO fiberSO;

        [Header("Scroll Settings")]
        [SerializeField] private float scrollDuration = 0.5f;

        public static int Wood { get; private set; }
        public static int Stone { get; private set; }
        public static int Fiber { get; private set; }
        public static int Population { get; private set; }
        public static int PopulationLimit { get; private set; }

        private int displayedWood;
        private int displayedStone;
        private int displayedFiber;

        private Coroutine woodCoroutine;
        private Coroutine stoneCoroutine;
        private Coroutine fiberCoroutine;

        private void Awake()
        {
            Bus<SupplyEvent>.OnEvent += HandleSuppliesUpdated;

            Wood = startingWood;
            Stone = startingStone;
            Fiber = startingFiber;
            Population = startingPopulation;
            PopulationLimit = startingPopulationLimit;

            displayedWood = Wood;
            displayedStone = Stone;
            displayedFiber = Fiber;

            UpdateUIImmediate();
        }

        private void OnDestroy()
        {
            Bus<SupplyEvent>.OnEvent -= HandleSuppliesUpdated;
        }

        private void UpdateUIImmediate()
        {
            if (woodText != null)
                woodText.text = displayedWood.ToString();

            if (stoneText != null)
                stoneText.text = displayedStone.ToString();

            if (fiberText != null)
                fiberText.text = displayedFiber.ToString();

            UpdatePopulationUI();
        }

        private void HandleSuppliesUpdated(SupplyEvent evt)
        {
            if (evt.Supply == woodSO)
            {
                AddWood(evt.Amount);
                return;
            }

            if (evt.Supply == stoneSO)
            {
                AddStone(evt.Amount);
                return;
            }

            if (evt.Supply == fiberSO)
            {
                AddFiber(evt.Amount);
            }
        }

        private void AddWood(int amount)
        {
            Wood += amount;
            StartScroll(ref woodCoroutine, displayedWood, Wood, value =>
            {
                displayedWood = value;

                if (woodText != null)
                    woodText.text = value.ToString();
            });
        }

        private void AddStone(int amount)
        {
            Stone += amount;
            StartScroll(ref stoneCoroutine, displayedStone, Stone, value =>
            {
                displayedStone = value;

                if (stoneText != null)
                    stoneText.text = value.ToString();
            });
        }

        private void AddFiber(int amount)
        {
            Fiber += amount;
            StartScroll(ref fiberCoroutine, displayedFiber, Fiber, value =>
            {
                displayedFiber = value;

                if (fiberText != null)
                    fiberText.text = value.ToString();
            });
        }

        public static bool HasPopulationSpace(int amount = 1)
        {
            return Population + amount <= PopulationLimit;
        }

        public void AddPopulation(int amount)
        {
            Population += amount;
            UpdatePopulationUI();
        }

        public void RemovePopulation(int amount)
        {
            Population = Mathf.Max(0, Population - amount);
            UpdatePopulationUI();
        }

        public void AddPopulationLimit(int amount)
        {
            PopulationLimit += amount;
            UpdatePopulationUI();
        }

        public void SetPopulation(int population, int populationLimit)
        {
            Population = Mathf.Max(0, population);
            PopulationLimit = Mathf.Max(0, populationLimit);
            UpdatePopulationUI();
        }

        private void UpdatePopulationUI()
        {
            if (populationText != null)
                populationText.text = $"{Population}/{PopulationLimit}";
        }

        private void StartScroll(
            ref Coroutine coroutine,
            int start,
            int target,
            Action<int> onUpdate)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutine = StartCoroutine(
                ScrollValue(start, target, scrollDuration, onUpdate));
        }

        private IEnumerator ScrollValue(
            int start,
            int target,
            float duration,
            Action<int> onUpdate)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                int current = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
                onUpdate?.Invoke(current);

                yield return null;
            }

            onUpdate?.Invoke(target);
        }
    }
}