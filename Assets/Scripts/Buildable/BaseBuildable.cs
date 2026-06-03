using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Buildable
{
    public class BaseBuildable : MonoBehaviour
    {
        [field: SerializeField] public MeshRenderer MainRenderer { get; private set; }

        [Header("Build Info")]
        [SerializeField] private Material PrimaryMaterial;
        [SerializeField] private LayerMask GridLayer;
        [SerializeField] private BuildableData buildableData;
        [SerializeField] protected bool IsActive;

        [SerializeField] private bool HasBuildingEffect;

        [SerializeField] private BuildableEffect[] Effects;

        [Header("Day Events")]
        [SerializeField] private bool RunOnDayChanged;
        [SerializeField, Min(1)] private int RunEveryXDays = 1;

        [Header("Day Period Events")]
        [SerializeField] private bool RunOnDayPeriodChanged;
        [SerializeField] private DayPeriod[] DayPeriodsToRun;

        [Header("Hour Events")]
        [SerializeField] private bool RunOnHourChanged;
        [SerializeField, Range(0, 23)] private int[] HoursToRun;

        [Header("Other Time Events")]
        [SerializeField] private bool RunOnDayStarted;
        [SerializeField] private bool RunOnTimeChanged;
        [SerializeField] private bool RunOnNightStarted;

        private void OnEnable()
        {
            Bus<DayChangedEvent>.OnEvent += HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent += HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent += HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent += HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        private void OnDisable()
        {
            Bus<DayChangedEvent>.OnEvent -= HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent -= HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent -= HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent -= HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
        }

        public void Build()
        {
            MainRenderer.material = PrimaryMaterial;
            IsActive = true;
        }

        public bool AllRestrictionsPass()
        {
            Ray ray = Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, math.INFINITY, GridLayer))
            {
                return false;
            }

            if (!hit.collider.TryGetComponent(out GrowBlock growBlock))
            {
                return false;
            }

            if (!buildableData.CanAfford())
            {
                return false;
            }

            return growBlock.IsActive;
        }

        protected virtual void BuildingEffect()
        {
            foreach (BuildableEffect effect in Effects)
            {
                effect.Apply(this);
            }
        }

        private void RunBuildingEffect()
        {
            if (!HasBuildingEffect)
            {
                return;
            }

            if (!IsActive)
            {
                return;
            }

            BuildingEffect();
        }

        private void HandleDayChanged(DayChangedEvent evt)
        {
            if (!RunOnDayChanged)
            {
                return;
            }

            if (evt.Day % RunEveryXDays == 0)
            {
                RunBuildingEffect();
            }
        }

        private void HandleDayStarted(DayStartedEvent evt)
        {
            if (RunOnDayStarted)
            {
                RunBuildingEffect();
            }
        }

        private void HandleDayPeriodChanged(DayPeriodChangedEvent evt)
        {
            if (!RunOnDayPeriodChanged)
            {
                return;
            }

            foreach (DayPeriod dayPeriod in DayPeriodsToRun)
            {
                if (evt.DayPeriod == dayPeriod)
                {
                    RunBuildingEffect();
                    return;
                }
            }
        }

        private void HandleHourChanged(HourChangedEvent evt)
        {
            if (!RunOnHourChanged)
            {
                return;
            }

            foreach (int hour in HoursToRun)
            {
                if (evt.Hour == hour)
                {
                    RunBuildingEffect();
                    return;
                }
            }
        }

        private void HandleTimeChanged(TimeChangedEvent evt)
        {
            if (RunOnTimeChanged)
            {
                RunBuildingEffect();
            }
        }

        private void HandleNightStarted(NightStartedEvent evt)
        {
            if (RunOnNightStarted)
            {
                RunBuildingEffect();
            }
        }
    }
}