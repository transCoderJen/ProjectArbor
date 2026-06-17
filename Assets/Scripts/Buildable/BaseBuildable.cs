using System.Collections.Generic;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
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
        public BuildableData BuildableData;
        public virtual Transform ProjectileSpawnPoint => transform;
        [SerializeField] protected bool IsActive;

        [Header("Ghost Preview")]
        [SerializeField] private Material GhostMaterial;

        [Header("Raid Target")]
        
        [SerializeField] private RaiderTargetType targetType = RaiderTargetType.Building;
        [SerializeField] private int RaidPriority = 50;

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
        [SerializeField] protected int MaxHP = 5;

        protected int hp;

        public GrowBlock OccupiedBlock { get; private set; }

        public CombatTeam Team => CombatTeam.Buildable;
        public Transform TargetTransform => transform;
        public RaiderTargetType TargetType => targetType;
        public int Priority => RaidPriority;
        public bool IsValidTarget => IsActive && hp > 0 && gameObject.activeInHierarchy;

        public int CurrentHP => hp;
        public int MaximumHP => MaxHP;

        private Renderer[] cachedRenderers;
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

        protected virtual void Awake()
        {
            CacheRenderersAndMaterials();

            hp = MaxHP;

            if (!IsActive)
                ApplyGhostMaterial();
        }

        protected virtual void OnEnable()
        {
            RaiderTargetRegistry.Register(this);

            Bus<DayChangedEvent>.OnEvent += HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent += HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent += HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent += HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        protected virtual void OnDisable()
        {
            RaiderTargetRegistry.Unregister(this);

            Bus<DayChangedEvent>.OnEvent -= HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent -= HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent -= HandleHourChanged;
            Bus<TimeChangedEvent>.OnEvent -= HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
        }

        protected virtual void Update()
        {
        }

        public virtual void Build()
        {
            RestoreOriginalMaterials();

            IsActive = true;
            hp = MaxHP;
        }

        public void SetOccupiedBlock(GrowBlock block)
        {
            OccupiedBlock = block;
        }

        private void CacheRenderersAndMaterials()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials.Clear();

            foreach (Renderer cachedRenderer in cachedRenderers)
            {
                if (!IsSupportedGhostRenderer(cachedRenderer))
                    continue;

                originalMaterials[cachedRenderer] = cachedRenderer.sharedMaterials;
            }
        }

        private bool IsSupportedGhostRenderer(Renderer targetRenderer)
        {
            if (targetRenderer == null)
                return false;

            return targetRenderer is MeshRenderer || targetRenderer is SpriteRenderer;
        }

        private void ApplyGhostMaterial()
        {
            if (GhostMaterial == null)
                return;

            foreach (Renderer cachedRenderer in cachedRenderers)
            {
                if (!IsSupportedGhostRenderer(cachedRenderer))
                    continue;

                Material[] currentMaterials = cachedRenderer.sharedMaterials;

                if (currentMaterials == null || currentMaterials.Length == 0)
                {
                    cachedRenderer.sharedMaterial = GhostMaterial;
                    continue;
                }

                Material[] ghostMaterials = new Material[currentMaterials.Length];

                for (int i = 0; i < ghostMaterials.Length; i++)
                    ghostMaterials[i] = GhostMaterial;

                cachedRenderer.sharedMaterials = ghostMaterials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (KeyValuePair<Renderer, Material[]> cachedRendererPair in originalMaterials)
            {
                Renderer cachedRenderer = cachedRendererPair.Key;

                if (!IsSupportedGhostRenderer(cachedRenderer))
                    continue;

                cachedRenderer.sharedMaterials = cachedRendererPair.Value;
            }
        }

        public bool AllRestrictionsPass()
        {
            Player player = PlayerManager.Instance.Player;

            GrowBlock growBlock =
                player.UsingController
                    ? GridManager.Instance.GetBlockController()
                    : GridManager.Instance.GetBlock();

            if (growBlock == null)
                return false;

            if (!growBlock.IsActive)
                return false;

            if (growBlock.HasBuildable)
                return false;

            if (BuildableData != null && !BuildableData.CanAfford())
                return false;

            return true;
        }

        protected virtual void BuildingEffect()
        {
            foreach (BuildableEffect effect in TimedEffects)
            {
                if (effect == null)
                    continue;

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

        public void TakeDamage(DamageData damageData)
        {
            if (!DamageRules.CanDamage(damageData.AttackerTeam, Team))
                return;

            DoDamage(damageData.Amount);
        }

        public virtual void DoDamage(int damage)
        {
            if (!IsActive)
                return;

            hp -= damage;

            if (hp <= 0)
                DestroyBuilding();
        }

        public void Heal(int amount)
        {
            if (hp <= 0)
                return;

            hp = Mathf.Min(hp + amount, MaxHP);
        }

        protected virtual void DestroyBuilding()
        {
            if (OccupiedBlock != null)
            {
                GrowBlock block = OccupiedBlock;

                OccupiedBlock.ClearBuildable(true);
                OccupiedBlock = null;

                block.UpdateGridInfo();

                if (this is FencePost2D)
                {
                    FencePost2D.RefreshNeighbors(block);
                }
            }

            Destroy(gameObject);
        }

        public virtual void RestoreFromSave(int savedHP)
        {
            RestoreOriginalMaterials();

            IsActive = true;

            hp = savedHP > 0
                ? Mathf.Clamp(savedHP, 1, MaxHP)
                : MaxHP;
        }

        protected virtual void OnDestroy()
        {
            RaiderTargetRegistry.Unregister(this);
        }
    }
}