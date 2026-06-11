using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Buildable
{
    public class BaseBuildable : MonoBehaviour
    {
        [field: SerializeField] public MeshRenderer MainRenderer { get; private set; }

        [Header("Build Info")]
        [SerializeField] private Material PrimaryMaterial;
        
        [SerializeField] private BuildableData buildableData;
        [SerializeField] protected bool IsActive;

        [SerializeField] private bool HasTimedEffects;
        [SerializeField] protected bool HasConstantEffects;

        [SerializeField] private BuildableEffect[] TimedEffects;
        [SerializeField] protected BuildableEffect[] ConstantEffects;

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

        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

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
            GrowBlock growBlock = PlayerManager.Instance.Player.PlayerInput.currentControlScheme == "Gamepad"
                    ? GridManager.Instance.GetBlockController()
                    : GridManager.Instance.GetBlock();

            if (growBlock == null)
                return false;

            if (!buildableData.CanAfford())
                return false;

            return growBlock.IsActive;
        }

        protected virtual void BuildingEffect()
        {
            foreach (BuildableEffect effect in TimedEffects)
            {
                effect.Apply(this);
            }
        }

        private void RunBuildingEffect()
        {
            if (!HasTimedEffects)
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

        public bool IsEffectReady(BuildableEffect effect, float cooldown)
        {
            if (effect == null)
                return false;

            if (!effectCooldowns.TryGetValue(effect, out float lastUsedTime))
                return true;

            return Time.time >= lastUsedTime + cooldown;
        }

        public void MarkEffectUsed(BuildableEffect effect)
        {
            if (effect == null)
                return;

            effectCooldowns[effect] = Time.time;
        }

        protected virtual void Update()
        {
            
        }
    }
}