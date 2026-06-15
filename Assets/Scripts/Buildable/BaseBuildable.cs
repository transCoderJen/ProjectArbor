using System.Collections.Generic;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class BaseBuildable : MonoBehaviour, IDamageable, IRaiderTarget, IHealable
    {
        [Header("Build Info")]
        [SerializeField] private BuildableData buildableData;
        [SerializeField] protected bool IsActive;

        [Header("Ghost Preview")]
        [SerializeField] private Material GhostMaterial;

        [Header("Effects")]
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

        [Header("Stats")]
        [SerializeField] protected float Durability;
        [SerializeField] protected int MaxHP;

        [Header("Raid Target")]
        [SerializeField] private RaiderTargetType targetType = RaiderTargetType.Building;
        [SerializeField] private int raidPriority = 50;
        [SerializeField] private Transform raidTargetPoint;

        public CombatTeam Team => CombatTeam.Buildable;
        public Transform TargetTransform => raidTargetPoint != null ? raidTargetPoint : transform;
        public RaiderTargetType TargetType => targetType;
        public int Priority => raidPriority;
        public bool IsValidTarget => IsActive && hp > 0 && gameObject.activeInHierarchy;

        protected int hp;

        private MeshRenderer[] meshRenderers;
        private readonly Dictionary<MeshRenderer, Material[]> originalMaterials = new();
        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

        private void Awake()
        {
            CacheRenderersAndMaterials();

            if (!IsActive)
            {
                ApplyGhostMaterial();
            }

            hp = MaxHP;
        }

        private void OnEnable()
        {
            RaiderTargetRegistry.Register(this);

            Bus<DayChangedEvent>.OnEvent += HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent += HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent += HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent += HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        private void OnDisable()
        {
            RaiderTargetRegistry.Unregister(this);
            
            Bus<DayChangedEvent>.OnEvent -= HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent -= HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent -= HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent -= HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
        }

        public void Build()
        {
            RestoreOriginalMaterials();
            IsActive = true;
        }

        public void Heal(int amount)
        {
            if (hp <= 0)
                return;

            hp = Mathf.Min(hp + amount, MaxHP);
        }

        private void CacheRenderersAndMaterials()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            originalMaterials.Clear();

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer == null)
                    continue;

                originalMaterials[meshRenderer] = meshRenderer.sharedMaterials;
            }
        }

        private void ApplyGhostMaterial()
        {
            if (GhostMaterial == null)
            {
                Debug.LogWarning($"Ghost Material is missing on {gameObject.name}.", this);
                return;
            }

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer == null)
                    continue;

                Material[] ghostMaterials = new Material[meshRenderer.sharedMaterials.Length];

                for (int i = 0; i < ghostMaterials.Length; i++)
                {
                    ghostMaterials[i] = GhostMaterial;
                }

                meshRenderer.sharedMaterials = ghostMaterials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (KeyValuePair<MeshRenderer, Material[]> cachedRenderer in originalMaterials)
            {
                MeshRenderer meshRenderer = cachedRenderer.Key;

                if (meshRenderer == null)
                    continue;

                meshRenderer.sharedMaterials = cachedRenderer.Value;
            }
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
                return;

            if (!IsActive)
                return;

            BuildingEffect();
        }

        private void HandleDayChanged(DayChangedEvent evt)
        {
            if (!RunOnDayChanged)
                return;

            if (evt.Day % RunEveryXDays == 0)
                RunBuildingEffect();
        }

        private void HandleDayStarted(DayStartedEvent evt)
        {
            if (RunOnDayStarted)
                RunBuildingEffect();
        }

        private void HandleDayPeriodChanged(DayPeriodChangedEvent evt)
        {
            if (!RunOnDayPeriodChanged)
                return;

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
                return;

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
                RunBuildingEffect();
        }

        private void HandleNightStarted(NightStartedEvent evt)
        {
            if (RunOnNightStarted)
                RunBuildingEffect();
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

        public virtual void DoDamage(int damage)
        {
            if (!IsActive)
                return;

            hp -= damage;

            if (hp <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public void TakeDamage(DamageData damageData)
        {
            if (!DamageRules.CanDamage(damageData.AttackerTeam, Team))
                return;

            DoDamage(damageData.Amount);
        }
    }
}