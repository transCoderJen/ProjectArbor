using System;
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
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Tools;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Components;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Buildable
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class BaseBuilding : AbstractCommandable, IInteractable, IContinuousInteractable
    {
       #region Configuration

        [Header("Build Info")]
        public BuildingSO UnitSO;

        protected override AbstractUnitSO config => UnitSO;

        #endregion

        #region Runtime State

        [SerializeField] protected bool IsActive;

        public GrowBlock OccupiedBlock { get; private set; }

        public virtual Transform ProjectileSpawnPoint => transform;

        #endregion

        #region Construction

        [Header("Building State / Progress")]
        [SerializeField] private ProgressBarWorld progressBarWorld;
        [SerializeField] private BuildingProgress progress = new();
        [SerializeField] private float buildInteractionDistance = 8f;

        public BuildingProgress Progress => progress;

        public bool IsComplete => progress.IsCompleted;
        public bool IsUnderConstruction => progress.IsBuilding;

        public float BuildProgress => progress.Progress;

        public float BuildTime =>
            UnitSO != null
                ? UnitSO.BuildTime
                : 0f;

        public float BuildProgressPercent =>
            BuildTime <= 0f
                ? 1f
                : Mathf.Clamp01(progress.Progress / BuildTime);

        #endregion

        #region Effects

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

        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

        #endregion

        #region Rendering Cacher

        private Collider[] solidColliders;
        private Renderer[] cachedRenderers;

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();

        #endregion

        #region Unit Production

        private const int MAX_QUEUE_SIZE = 5;

        private readonly List<UnlockableSO> buildingQueue = new(MAX_QUEUE_SIZE);

        public int QueueSize => buildingQueue.Count;
        public UnlockableSO[] Queue => buildingQueue.ToArray();

        public float CurrentQueueStartTime { get; private set; }
        public UnlockableSO SOBeingBuilt { get; private set; }

        public delegate void QueueUpdatedEvent(UnlockableSO[] unitsInQueue);

        public event QueueUpdatedEvent OnQueueUpdated;
        public event Action<float, float> OnBuildProgressUpdated;
        public event System.Action OnBuildCompleted;

        #endregion

        #region Builder Assignment

        [SerializeField] private int maxBuilders = 3;

        private readonly List<Worker> assignedBuilders = new();

        public bool HasBuilderSlot => assignedBuilders.Count < maxBuilders;

        public float BuilderMultiplier =>
            Mathf.Max(1, assignedBuilders.Count);

        #endregion

        private int unitsSpawnedCount;


        #region Unity Lifecycle

        protected virtual void Awake()
        {
            CacheRenderersAndMaterials();
            CacheSolidCollidersIfNeeded();

            if (progressBarWorld != null)
            {
                progressBarWorld.SetTarget(transform);
                progressBarWorld.gameObject.SetActive(false);
            }
        }

        override protected void Start()
        {
            base.Start();
            
            // foreach(UpgradeSO upgrade in UnitSO.Upgrades)
            // {
            //     if (UnitSO.TechTree.IsResearched(upgrade))
            //     {
            //         upgrade.Apply(UnitSO);
            //     }
            // }
        }

        protected virtual void Update()
        {

        }

        protected virtual void OnEnable()
        {
            // RaiderTargetRegistry.Register(this);

            Bus<DayChangedEvent>.OnEvent += HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent += HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent += HandleHourChanged;
            Bus<FiveMinuteTickEvent>.OnEvent += HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        protected virtual void OnDisable()
        {
            // RaiderTargetRegistry.Unregister(this);

            Bus<DayChangedEvent>.OnEvent -= HandleDayChanged;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;
            Bus<DayPeriodChangedEvent>.OnEvent -= HandleDayPeriodChanged;
            Bus<HourChangedEvent>.OnEvent -= HandleHourChanged;
            Bus<FiveMinuteTickEvent>.OnEvent -= HandleTimeChanged;
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
        }

        private void HandleBuildProgressUpdated(float current, float max)
        {
            if (progressBarWorld == null)
            {
                Debug.LogWarning($"{name} progressBarWorld is null");
                return;
            }
            
            progressBarWorld.gameObject.SetActive(true);

            float progress = max > 0f ? current / max : 1f;
            progressBarWorld.SetProgress(progress);
        }

        private void HandleBuildCompleted()
        {
            if (progressBarWorld != null)
                progressBarWorld.gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            // RaiderTargetRegistry.Unregister(this);
            Bus<BuildingDeathEvent>.Raise(new BuildingDeathEvent(this));
        }

        #endregion

        #region Construction

        public virtual void PlaceAsConstructionSite()
        {
            if (progressBarWorld != null)
            {
                progressBarWorld.SetProgress(0f);
                progressBarWorld.gameObject.SetActive(false);
            }

            progress.Start();

            IsActive = false;

            SetHealth(MaxHealth, MaxHealth);
            ShowGhostVisuals();
            SetSolid(false);

            Bus<BuildingPlacedForConstructionEvent>.Raise(
                new BuildingPlacedForConstructionEvent(this));
        }

       public virtual void AddBuildProgress(
            Worker worker,
            float amount)
        {
            if (!progress.IsBuilding)
                return;

            if (worker == null)
                return;

            float distance = Vector3.Distance(
                worker.transform.position,
                transform.position);

            if (distance > buildInteractionDistance)
                return;

            AddBuildProgressInternal(amount);
        }

        public virtual void AddBuildProgress(float amount)
        {
            if (!progress.IsBuilding)
                return;

            AddBuildProgressInternal(amount);
        }

        private void AddBuildProgressInternal(float amount)
        {
            progress.AddProgress(amount, BuildTime);

            HandleBuildProgressUpdated(
                progress.Progress,
                BuildTime);

            OnBuildProgressUpdated?.Invoke(
                progress.Progress,
                BuildTime);

            if (progress.IsCompleted)
                FinishBuilding();
        }

        public virtual void CompleteBuilding()
        {
            if (!progress.IsCompleted)
            {
                progress.Complete(BuildTime);

                HandleBuildProgressUpdated(
                    progress.Progress,
                    BuildTime);

                OnBuildProgressUpdated?.Invoke(
                    progress.Progress,
                    BuildTime);
            }

            FinishBuilding();
        }

        private void FinishBuilding()
        {
            HandleBuildCompleted();
            OnBuildCompleted?.Invoke();

            RestoreOriginalMaterials();
            SetSolid(true);

            IsActive = true;
            SetHealth(MaxHealth, MaxHealth);

            Bus<BuildingSpawnEvent>.Raise(new BuildingSpawnEvent(this));

            if (OccupiedBlock != null)
                OccupiedBlock.UpdateGridInfo();
        }

        public virtual void Build()
        {
            CompleteBuilding();
        }

        #endregion

        #region Construction Workers

        public bool TryAssignBuilder(Worker worker)
        {
            if (worker == null)
                return false;

            if (!IsUnderConstruction)
                return false;

            if (assignedBuilders.Contains(worker))
                return true;

            if (!HasBuilderSlot)
                return false;

            assignedBuilders.Add(worker);
            return true;
        }

        public void ReleaseBuilder(Worker worker)
        {
            if (worker == null)
                return;

            assignedBuilders.Remove(worker);
        }

        #endregion

        #region Collision / Solid State

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

        #endregion

        #region Occupied Block

        public void SetOccupiedBlock(GrowBlock block)
        {
            OccupiedBlock = block;
        }

        #endregion

        #region Ghost Visuals

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

        #endregion

        #region Build Restrictions

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

        #endregion

        #region Effects

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

        #endregion

        #region Effect Cooldowns

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

        #endregion

        #region Damage / Health

        public override void TakeDamage(DamageData damageData)
        {
            Debug.Log(
                $"{name} building hit | " +
                $"IsActive: {IsActive} | " +
                $"AttackerTeam: {damageData.Owner} | " +
                $"TargetTeam: {Owner} | " +
                $"CanDamageBuildables: {damageData.CanDamageBuildables}");

            if (!IsActive)
                return;

            base.TakeDamage(damageData);
        }

        public override void Heal(int amount)
        {
            if (!IsActive)
                return;

            base.Heal(amount);
        }

        protected override void Die()
        {
            DestroyBuilding();
        }

        protected virtual void DestroyBuilding()
        {
            progress.MarkDestroyed();

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

        #endregion

        #region Save / Load

        public virtual void RestoreFromSave(int savedHP)
        {
            progress.Complete(BuildTime);

            RestoreOriginalMaterials();
            SetSolid(true);

            IsActive = true;

            int restoredHP = savedHP > 0
                ? Mathf.Clamp(savedHP, 1, MaxHealth)
                : MaxHealth;

            SetHealth(restoredHP, MaxHealth);

            if (progressBarWorld != null)
            {
                progressBarWorld.SetProgress(1f);
                progressBarWorld.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Unit Queue

        public void BuildUnlockable(UnlockableSO unlockable)
        {   
            if (buildingQueue.Count == MAX_QUEUE_SIZE)
                return;

            unlockable.SupplyCost.Spend();
            buildingQueue.Add(unlockable);

            if (buildingQueue.Count == 1)
                StartCoroutine(DoBuildUnits());
            else
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());
        }

        public void CancelBuildingUnit(int index)
        {
            if (index < 0 || index >= buildingQueue.Count)
                return;

            RefundCost(index);

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

        private void RefundCost(int index)
        {
            if (index < 0 || index >= buildingQueue.Count)
                return;

            buildingQueue[index]?.SupplyCost?.Refund();
        }

        private IEnumerator DoBuildUnits()
        {
            while (buildingQueue.Count > 0)
            {
                SOBeingBuilt = buildingQueue[0];
                CurrentQueueStartTime = Time.time;
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());

                yield return Helpers.GetWait(SOBeingBuilt.BuildTime);

                if (SOBeingBuilt is AbstractUnitSO unitSO)
                {
                    Vector3 spawnPosition = GetUnitSpawnPosition();
                    Instantiate((SOBeingBuilt as AbstractUnitSO).Prefab, spawnPosition, Quaternion.identity);
                }
                else if (SOBeingBuilt is UpgradeSO upgrade)
                {
                    Bus<UpgradeResearchEvent>.Raise(new UpgradeResearchEvent(upgrade));
                }

                buildingQueue.RemoveAt(0);
            }

            SOBeingBuilt = null;
            CurrentQueueStartTime = 0f;
            OnQueueUpdated?.Invoke(buildingQueue.ToArray());
        }

        private Vector3 GetUnitSpawnPosition()
        {
            float radius = 1.5f;
            float angle = unitsSpawnedCount * 137.5f * Mathf.Deg2Rad;

            Vector3 position = transform.position + new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)) * radius;

            unitsSpawnedCount++;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;

            return position;
        }

        #endregion

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

            // Open Building UI
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