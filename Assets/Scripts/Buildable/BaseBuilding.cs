using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class BaseBuilding : AbstractCommandable, IRaiderTarget
    {
        #region OTher Variables
        [Header("Build Info")]
        public BuildableData UnitSO;
        protected override AbstractUnitSO Config => UnitSO;
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

        public GrowBlock OccupiedBlock { get; private set; }

        public override CombatTeam Team => CombatTeam.Buildable;
        public Transform TargetTransform => transform;
        public RaiderTargetType TargetType => targetType;
        public int Priority => RaidPriority;
        public bool IsValidTarget => IsActive && CurrentHealth > 0 && gameObject.activeInHierarchy;

        private Renderer[] cachedRenderers;
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private readonly Dictionary<BuildableEffect, float> effectCooldowns = new();

        #endregion

        #region RTS Variables

        public int QueueSize => buildingQueue.Count;
        public AbstractUnitSO[] Queue => buildingQueue.ToArray();
        [field: SerializeField] public float CurrentQueueStartTime { get; private set; }
        [field: SerializeField] public AbstractUnitSO BuildingUnit { get; private set; }

        public delegate void QueueUpdatedEvent(AbstractUnitSO[] unitsInQueue);
        public event QueueUpdatedEvent OnQueueUpdated;

        private List<AbstractUnitSO> buildingQueue = new (MAX_QUEUE_SIZE);
        private const int MAX_QUEUE_SIZE = 5;
        #endregion

        protected virtual void Awake()
        {
            CacheRenderersAndMaterials();

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

        #region Other
        public virtual void Build()
        {
            RestoreOriginalMaterials();

            IsActive = true;
            SetHealth(MaxHealth, MaxHealth);
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

            int restoredHP = savedHP > 0
                ? Mathf.Clamp(savedHP, 1, MaxHealth)
                : MaxHealth;

            SetHealth(restoredHP, MaxHealth);
        }

        protected virtual void OnDestroy()
        {
            RaiderTargetRegistry.Unregister(this);
        }
        #endregion

        #region RTS
        public void BuildUnit(AbstractUnitSO unit)
        {
            if (buildingQueue.Count == MAX_QUEUE_SIZE)
            {
                // Debug.LogError("BuildUnit called when the queue was already full!  This is not supported!");
                return;
            }

            buildingQueue.Add(unit);

            if (buildingQueue.Count == 1)
            {
                StartCoroutine(DoBuildUnits());
            }
            else
            {
                OnQueueUpdated?.Invoke(buildingQueue.ToArray());
            }
        }

        public void CancelBuildingUnit(int index)
        {
            if (index < 0 || index > buildingQueue.Count)
            {
                // Debug.LogError("Attempting to cacncel building a unit outside the bounds of the queue!");
                return;
            }

            buildingQueue.RemoveAt(index);
            if (index == 0)
            {
                StopAllCoroutines();

                if (buildingQueue.Count > 0)
                {
                    StartCoroutine(DoBuildUnits());
                }
                else
                {
                    OnQueueUpdated?.Invoke(buildingQueue.ToArray());
                }
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
        #endregion
    }
}