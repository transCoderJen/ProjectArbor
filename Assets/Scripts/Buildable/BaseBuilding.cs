using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Tools;
using ShiftedSignal.Garden.Units;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Buildable
{
    public enum BuildingState
    {
        Preview,
        UnderConstruction,
        Complete
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class BaseBuilding : AbstractCommandable, IRaiderTarget, IInteractable, IContinuousInteractable
    {
        [Header("Build Info")]
        public BuildingSO UnitSO;
        protected override AbstractUnitSO Config => UnitSO;
        public virtual Transform ProjectileSpawnPoint => transform;

        [SerializeField] protected bool IsActive;

        [Header("Building State / Progress")]
        [SerializeField] private BuildingState buildingState = BuildingState.Complete;
        [SerializeField] private float buildTime = 3f;

        private Collider[] solidColliders;
        private float buildProgress;

        public BuildingState CurrentBuildingState => buildingState;
        public bool IsComplete => buildingState == BuildingState.Complete;
        public bool IsUnderConstruction => buildingState == BuildingState.UnderConstruction;
        public float BuildProgress => buildProgress;
        public float BuildTime => buildTime;
        public float BuildProgressPercent => buildTime <= 0f ? 1f : Mathf.Clamp01(buildProgress / buildTime);

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

        public GrowBlock OccupiedBlock { get; private set; }

        public override CombatTeam Team => CombatTeam.Buildable;
        public Transform TargetTransform => transform;
        public RaiderTargetType TargetType => targetType;
        public int Priority => RaidPriority;
        public bool IsValidTarget => IsActive && CurrentHealth > 0 && gameObject.activeInHierarchy;

        private Renderer[] cachedRenderers;
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

        public int QueueSize => buildingQueue.Count;
        public AbstractUnitSO[] Queue => buildingQueue.ToArray();

        [field: SerializeField] public float CurrentQueueStartTime { get; private set; }
        [field: SerializeField] public AbstractUnitSO BuildingUnit { get; private set; }

        public delegate void QueueUpdatedEvent(AbstractUnitSO[] unitsInQueue);
        public event QueueUpdatedEvent OnQueueUpdated;

        private readonly List<AbstractUnitSO> buildingQueue = new(MAX_QUEUE_SIZE);
        private const int MAX_QUEUE_SIZE = 5;

        protected virtual void Awake()
        {
            CacheRenderersAndMaterials();
            CacheSolidCollidersIfNeeded();
        }

        protected virtual void OnEnable()
        {
            RaiderTargetRegistry.Register(this);

            Bus<DayChangedEvent>.OnEvent += HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent += HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent += HandleHourChanged;
            Bus<FiveMinuteTickEvent>.OnEvent += HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        protected virtual void OnDisable()
        {
            RaiderTargetRegistry.Unregister(this);

            Bus<DayChangedEvent>.OnEvent -= HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent -= HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent -= HandleHourChanged;
            Bus<FiveMinuteTickEvent>.OnEvent -= HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
        }

        public virtual void PlaceAsConstructionSite()
        {
            buildingState = BuildingState.UnderConstruction;
            buildProgress = 0f;

            IsActive = false;

            SetHealth(MaxHealth, MaxHealth);
            ShowGhostVisuals();
            SetSolid(false);
        }

        public virtual void AddBuildProgress(float amount)
        {
            if (!IsUnderConstruction)
                return;

            buildProgress += amount;

            if (buildProgress >= buildTime)
            {
                CompleteBuilding();
            }
        }

        public virtual void CompleteBuilding()
        {
            buildingState = BuildingState.Complete;
            buildProgress = buildTime;

            RestoreOriginalMaterials();
            SetSolid(true);

            IsActive = true;
            SetHealth(MaxHealth, MaxHealth);

            if (OccupiedBlock != null)
                OccupiedBlock.UpdateGridInfo();
        }

        public virtual void Build()
        {
            CompleteBuilding();
        }

        protected virtual void Update()
        {
            
        }

        private void SetSolid(bool solid)
        {
            CacheSolidCollidersIfNeeded();

            foreach (Collider solidCollider in solidColliders)
            {
                if (solidCollider != null)
                    solidCollider.enabled = solid;
            }

            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();

            if (obstacle != null)
                obstacle.enabled = solid;
        }

        private void CacheSolidCollidersIfNeeded()
        {
            if (solidColliders != null && solidColliders.Length > 0)
                return;

            Collider[] allColliders = GetComponentsInChildren<Collider>(true);

            List<Collider> solids = new();

            foreach (Collider collider in allColliders)
            {
                if (collider == null)
                    continue;

                if (collider.isTrigger)
                    continue;

                solids.Add(collider);
            }

            solidColliders = solids.ToArray();
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

        public void ShowGhostVisuals()
        {
            if (UnitSO == null || UnitSO.GhostMaterial == null)
                return;

            foreach (Renderer cachedRenderer in cachedRenderers)
            {
                if (!IsSupportedGhostRenderer(cachedRenderer))
                    continue;

                Material[] currentMaterials = cachedRenderer.sharedMaterials;

                if (currentMaterials == null || currentMaterials.Length == 0)
                {
                    cachedRenderer.sharedMaterial = UnitSO.GhostMaterial;
                    continue;
                }

                Material[] ghostMaterials = new Material[currentMaterials.Length];

                for (int i = 0; i < ghostMaterials.Length; i++)
                    ghostMaterials[i] = UnitSO.GhostMaterial;

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
            Player player = Player.Instance;

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

            if (UnitSO != null && !UnitSO.CanAfford())
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

        private void HandleTimeChanged(FiveMinuteTickEvent evt)
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

        public override void TakeDamage(DamageData damageData)
        {
            if (!IsActive)
                return;

            base.TakeDamage(damageData);
        }

        public override void DoDamage(int damage)
        {
            if (!IsActive)
                return;

            base.DoDamage(damage);
        }

        protected override void Die()
        {
            DestroyBuilding();
        }

        public override void Heal(int amount)
        {
            if (!IsActive)
                return;

            base.Heal(amount);
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
                    FencePost2D.RefreshNeighbors(block);
            }

            Destroy(gameObject);
        }

        public virtual void RestoreFromSave(int savedHP)
        {
            buildingState = BuildingState.Complete;
            buildProgress = buildTime;

            RestoreOriginalMaterials();
            SetSolid(true);

            IsActive = true;

            int restoredHP = savedHP > 0
                ? Mathf.Clamp(savedHP, 1, MaxHealth)
                : MaxHealth;

            SetHealth(restoredHP, MaxHealth);
        }

        protected virtual void OnDestroy()
        {
            RaiderTargetRegistry.Unregister(this);
        }

        public void BuildUnit(AbstractUnitSO unit)
        {
            if (buildingQueue.Count == MAX_QUEUE_SIZE)
                return;

            buildingQueue.Add(unit);

            if (buildingQueue.Count == 1)
                StartCoroutine(DoBuildUnits());
            else
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());
        }

        public void CancelBuildingUnit(int index)
        {
            if (index < 0 || index >= buildingQueue.Count)
                return;

            buildingQueue.RemoveAt(index);

            if (index == 0)
            {
                StopAllCoroutines();

                if (buildingQueue.Count > 0)
                    StartCoroutine(DoBuildUnits());
                else
                    OnQueueUpdated?.Invoke(buildingQueue.ToArray());
            }
            else
            {
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());
            }
        }

        private IEnumerator DoBuildUnits()
        {
            while (buildingQueue.Count > 0)
            {
                BuildingUnit = buildingQueue[0];
                CurrentQueueStartTime = Time.time;
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());

                yield return Helpers.GetWait(BuildingUnit.BuildTime);

                Instantiate(BuildingUnit.Prefab, transform.position, Quaternion.identity);
                buildingQueue.RemoveAt(0);
            }

            OnQueueUpdated?.Invoke(buildingQueue.ToArray());
        }

        #region Interact

        public void Highlight(bool highlight)
        {
            if (!IsUnderConstruction)
                return;

            // Temporary: construction sites already show ghost visuals.
            // Later we can add outline/highlight feedback here.
        }

        public void Interact(Player player)
        {
            if (!IsUnderConstruction)
                return;

            // Open Buyilding UI
        }

        public void ContinuousInteract(Player player)
        {
            if (!IsUnderConstruction)
            {
                return;
            }

            AddBuildProgress(Time.deltaTime);
        }

        #endregion
    }
}