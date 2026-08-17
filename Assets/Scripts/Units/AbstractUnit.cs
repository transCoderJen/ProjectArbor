using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.TechTree;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(NavMeshAgent), typeof(BehaviorGraphAgent))]
    public abstract class AbstractUnit : AbstractCommandable, IMoveable, IAttacker
    {
        [Header("Unit Config")]
        protected override AbstractUnitSO config => UnitSO;
        public AbstractUnitSO UnitData => UnitSO;
        [SerializeField] private AbstractUnitSO UnitSO;
        [SerializeField] private float AnimationUpdateRate = 0.1f;
        [SerializeField] private string instanceID;
        [SerializeField] private DamageableSensor DameagableSensor;
        public virtual Transform ProjectileSpawnPoint => transform;
        public string InstanceID => instanceID;
        
        public void SetInstanceID(string id)
        {
            instanceID = id;
        }

        public UnitCommands CurrentCommand
        {
            get
            {
                if (graphAgent != null &&
                    graphAgent.GetVariable("Command", out BlackboardVariable<UnitCommands> commandVariable))
                {
                    return commandVariable.Value;
                }

                return UnitCommands.Stop;
            }
        }

        public bool IsInCombat
        {
            get
            {
                GameObject target = CurrentTarget;

                if (target == null)
                    return false;

                IDamageable damageable =
                    target.GetComponentInParent<IDamageable>();

                return damageable != null &&
                    damageable.CurrentHealth > 0;
            }
        }

        public GameObject CurrentTarget
        {
            get
            {
                if (graphAgent == null)
                    return null;

                if (!graphAgent.GetVariable(
                        "TargetGameObject",
                        out BlackboardVariable<GameObject> targetVariable))
                {
                    return null;
                }

                return targetVariable.Value;
            }
        }   

        public GameObject ActiveCombatTarget
        {
            get
            {
                GameObject retaliationTarget =
                    GetBlackboardGameObject(
                        "RetaliationTarget");

                if (IsLivingTarget(
                        retaliationTarget))
                {
                    return retaliationTarget;
                }

                if (IsLivingTarget(
                        CurrentTarget))
                {
                    return CurrentTarget;
                }

                return null;
            }
        }

        public GameObject RetaliationTarget
        {
            get
            {
                if (graphAgent == null)
                    return null;

                if (!graphAgent.GetVariable(
                        "RetaliationTarget",
                        out BlackboardVariable<GameObject> variable))
                {
                    return null;
                }

                return variable.Value;
            }
        }

        public float AgentRadius => agent.radius;
        private NavMeshAgent agent;
        private Animator anim;

        protected BehaviorGraphAgent graphAgent;

        private float lastAnimX;
        private float lastAnimY;
        private float nextAnimationUpdateTime;

        // private Vector3 pursuitOrigin;
        // private bool hasPursuitOrigin;

        protected virtual bool AutoAcquireNearbyTargets => false;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();

            if (string.IsNullOrEmpty(instanceID))
            {
                instanceID = System.Guid.NewGuid().ToString();
            }
            
            agent.updateRotation = false;

            graphAgent = GetComponent<BehaviorGraphAgent>();

            if (graphAgent.GetVariable("Command", 
                out BlackboardVariable<UnitCommands> commandVariable))
            {
                graphAgent.SetVariableValue(
                    "Command",
                    UnitCommands.Stop);
            }

            graphAgent.SetVariableValue("AttackConfig", UnitSO.AttackConfig);
        }

        protected override void Start()
        {
            base.Start();

            Bus<UnitSpawnEvent>.Raise(new UnitSpawnEvent(this));
            Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;

            if (DameagableSensor != null)
            {
                DameagableSensor.OnUnitEnter += HandleUnitEnter;
                DameagableSensor.OnUnitExit += HandleUnitExit;
                DameagableSensor.SetupFrom(UnitSO.AttackConfig, Owner);
            }

            graphAgent.SetVariableValue("Farm", PlayerManager.Instance.FarmGameObject);

            // foreach(UpgradeSO upgrade in UnitSO.Upgrades)
            // {
            //     if (UnitSO.TechTree.IsResearched(upgrade))
            //     {
            //         upgrade.Apply(UnitSO);
            //     }
            // }
        }

        protected virtual void OnDestroy()
        {
            
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;

            if (Application.isPlaying)
                Bus<UnitDeathEvent>.Raise(new UnitDeathEvent(this));

            if (DameagableSensor != null)
            {
                DameagableSensor.OnUnitEnter -= HandleUnitEnter;
                DameagableSensor.OnUnitExit -= HandleUnitExit;
            }
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
{
    if (graphAgent == null ||
        !graphAgent.isActiveAndEnabled ||
        !gameObject.activeInHierarchy)
    {
        return;
    }

    // Do not process this unit's own death event.
    if (evt.Unit == null ||
        evt.Unit == this)
    {
        return;
    }

    if (!graphAgent.GetVariable(
            "TargetGameObject",
            out BlackboardVariable<GameObject> targetVariable))
    {
        return;
    }

    GameObject currentTarget =
        targetVariable.Value;

    if (currentTarget == null)
        return;

    // Only react if the unit that died was our current target.
    if (evt.Unit.gameObject != currentTarget)
        return;

    List<GameObject> nearbyEnemies =
        SetNearbyEnemiesOnBlackboard();

    GameObject nextTarget =
        GetNextNonBuildingTarget(
            nearbyEnemies);

    if (nextTarget != null)
    {
        IDamageable nextDamageable =
            nextTarget.GetComponentInParent<IDamageable>();

        Debug.Log(
            $"[TARGET SET] {name} | " +
            $"Source=HandleUnitDeath | " +
            $"PreviousTarget={currentTarget.name} | " +
            $"Target={nextTarget.name} | " +
            $"Owner={(nextDamageable != null ? nextDamageable.Owner.ToString() : "NO IDAMAGEABLE")}");

        graphAgent.SetVariableValue(
            "TargetGameObject",
            nextTarget);

        graphAgent.SetVariableValue(
            "Command",
            UnitCommands.Attack);

        return;
    }

    Debug.Log(
        $"[TARGET SET] {name} | " +
        $"Source=HandleUnitDeath | " +
        $"PreviousTarget={currentTarget.name} | " +
        $"Target=NULL");

    graphAgent.SetVariableValue<GameObject>(
        "TargetGameObject",
        null);

    if (IsAttackMoveActive())
        return;

    graphAgent.SetVariableValue(
        "Command",
        UnitCommands.Stop);
}

        protected virtual void Update()
        {
            if(Helpers.EveryXFrames(10))
                UpdateMovementAnimation();
        }

        private GameObject GetBlackboardGameObject(
            string variableName)
        {
            if (graphAgent == null)
                return null;

            if (!graphAgent.GetVariable(
                    variableName,
                    out BlackboardVariable<GameObject> variable))
            {
                return null;
            }

            return variable.Value;
        }

        private bool IsLivingTarget(
            GameObject target)
        {
            if (target == null)
                return false;

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            return damageable != null &&
                damageable.CurrentHealth > 0;
        }

        private void HandleUnitEnter(IDamageable damageable)
        {
            bool attackMove = IsAttackMoveActive();

            List<GameObject> nearbyEnemies =
                SetNearbyEnemiesOnBlackboard();

            // Normal units only automatically acquire targets
            // while performing an Attack Move.
            //
            // Military units automatically acquire nearby threats.
            if (!attackMove &&
                !AutoAcquireNearbyTargets)
            {
                return;
            }

            if (!graphAgent.GetVariable(
                    "TargetGameObject",
                    out BlackboardVariable<GameObject> targetVariable))
            {
                Debug.LogWarning(
                    $"{name} could not find TargetGameObject blackboard variable.");

                return;
            }

            // Already fighting something.
            // Don't change targets yet.
            if (targetVariable.Value != null)
                return;

            GameObject nextTarget =
                GetNextNonBuildingTarget(nearbyEnemies);

            if (nextTarget == null)
                return;

            graphAgent.SetVariableValue(
                "TargetGameObject",
                nextTarget);

            graphAgent.SetVariableValue(
                "Command",
                UnitCommands.Attack);
        }

        private void HandleUnitExit(IDamageable damageable)
        {
            bool attackMove =
                IsAttackMoveActive();

            if (!attackMove &&
                !AutoAcquireNearbyTargets)
            {
                return;
            }

            GameObject exitingTarget = null;
            Vector3 lastTargetPosition =
                transform.position;

            if (damageable is UnityEngine.Object unityObject &&
                unityObject != null)
            {
                exitingTarget =
                    damageable.Transform.gameObject;

                lastTargetPosition =
                    damageable.Transform.position;
            }

            List<GameObject> nearbyEnemies =
                SetNearbyEnemiesOnBlackboard();

            if (!graphAgent.GetVariable(
                    "TargetGameObject",
                    out BlackboardVariable<GameObject> targetVariable))
            {
                return;
            }

            if (exitingTarget == null ||
                exitingTarget != targetVariable.Value)
            {
                return;
            }

            /*
            * Military units keep pursuing their current
            * target even after it leaves the sensor.
            *
            * The sensor is used for acquiring targets,
            * not for determining chase distance.
            */
            if (AutoAcquireNearbyTargets)
            {
                return;
            }

            /*
            * Existing Attack Move behavior.
            */
            GameObject nextTarget =
                GetNextNonBuildingTarget(
                    nearbyEnemies);

            if (nextTarget != null)
            {
                graphAgent.SetVariableValue(
                    "TargetGameObject",
                    nextTarget);

                graphAgent.SetVariableValue(
                    "Command",
                    UnitCommands.Attack);

                return;
            }

            graphAgent.SetVariableValue<GameObject>(
                "TargetGameObject",
                null);

            graphAgent.SetVariableValue(
                "TargetLocation",
                lastTargetPosition);
        }

        private GameObject GetNextNonBuildingTarget(List<GameObject> nearbyEnemies)
        {
            foreach (GameObject enemy in nearbyEnemies)
            {
                if (enemy == null)
                    continue;

                if (enemy.GetComponentInParent<BaseBuilding>() != null)
                    continue;

                return enemy;
            }

            return null;
        }

        private List<GameObject> SetNearbyEnemiesOnBlackboard()
        {
            List<GameObject> nearbyEnemies = new();

            foreach (IDamageable target in DameagableSensor.Damageables)
            {
                if (target.CurrentHealth <= 0)
                    continue;

                if (target == null)
                {
                    continue;
                }

                if (target is UnityEngine.Object unityObject && unityObject == null)
                {
                    continue;
                }

                if (target.Owner == Owner)
                {
                    continue;
                }

                GameObject targetObject = target.Transform.gameObject;                

                nearbyEnemies.Add(targetObject);
            }

            nearbyEnemies.Sort(new DamageableTargetGameObjectComparer(transform.position));

            graphAgent.SetVariableValue("NearbyEnemies", nearbyEnemies);

            return nearbyEnemies;
        }

        #region Move / Stop

        public virtual void MoveTo(Vector3 position)
        {
            if (agent != null)
                agent.isStopped = false;

            graphAgent.SetVariableValue("IsAttackMove", false);    
            graphAgent.SetVariableValue("TargetLocation", position);
            graphAgent.SetVariableValue("Command", UnitCommands.Move);
        }

        public virtual void Stop()
        {
            graphAgent.SetVariableValue("IsAttackMove", false);
            graphAgent.SetVariableValue("Command", UnitCommands.Stop);
        }

        protected virtual void UpdateMovementAnimation()
        {
            if (anim == null || agent == null)
                return;

            if (Time.time < nextAnimationUpdateTime)
                return;

            nextAnimationUpdateTime = Time.time + AnimationUpdateRate;

            Vector2 moveDirection = new Vector2(agent.velocity.x, agent.velocity.z);

            anim.SetFloat(AnimationConstants.SPEED, moveDirection.magnitude);

            if (moveDirection.sqrMagnitude < 0.01f)
                return;

            SetMovementDirection(moveDirection);
        }

        public void SetRotation(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;

            SetMovementDirection(new Vector2(forward.x, forward.z));
        }

        private void SetMovementDirection(Vector2 direction)
        {
            if (anim == null)
                return;

            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();

            float animX = Mathf.Abs(direction.x) > 0.5f
                ? Mathf.Sign(direction.x)
                : 0f;

            float animY = Mathf.Abs(direction.y) > 0.5f
                ? Mathf.Sign(direction.y)
                : 0f;

            if (animX == 0f && animY == 0f)
                return;

            if (animX != lastAnimX)
            {
                anim.SetFloat(AnimationConstants.MOVEMENTX, animX);
                lastAnimX = animX;
            }

            if (animY != lastAnimY)
            {
                anim.SetFloat(AnimationConstants.MOVEMENTY, animY);
                lastAnimY = animY;
            }
        }

#endregion

#region Attack/Defend
        public void Attack(IDamageable damageable)
        {
            if (agent != null)
                agent.isStopped = false;

            graphAgent.SetVariableValue(
                "IsAttackMove",
                false);

            graphAgent.SetVariableValue(
                "TargetGameObject",
                damageable.Transform.gameObject);

            graphAgent.SetVariableValue(
                "Command",
                UnitCommands.Attack);
        }

        public void Attack(Vector3 location)
        {
            if (agent != null)
                agent.isStopped = false;

            Debug.Log(
                $"[ATTACK MOVE] {name} to {location}");

            graphAgent.SetVariableValue(
                "IsAttackMove",
                true);

            graphAgent.SetVariableValue<GameObject>(
                "TargetGameObject",
                null);

            graphAgent.SetVariableValue(
                "TargetLocation",
                location);

            graphAgent.SetVariableValue(
                "Command",
                UnitCommands.Attack);

            if (graphAgent.GetVariable(
                    "Command",
                    out BlackboardVariable<UnitCommands> command))
            {
                Debug.Log(
                    $"[ATTACK MOVE] Command={command.Value}");
            }

            if (graphAgent.GetVariable(
                    "IsAttackMove",
                    out BlackboardVariable<bool> attackMove))
            {
                Debug.Log(
                    $"[ATTACK MOVE] IsAttackMove={attackMove.Value}");
            }

            if (graphAgent.GetVariable(
                    "TargetLocation",
                    out BlackboardVariable<Vector3> targetLocation))
            {
                Debug.Log(
                    $"[ATTACK MOVE] TargetLocation={targetLocation.Value}");
            }
        }

        private bool IsAttackMoveActive()
        {
            return graphAgent.GetVariable("IsAttackMove", out BlackboardVariable<bool> variable)
                && variable.Value;
        }

        public override void TakeDamage(DamageData damageData)
        {
            base.TakeDamage(damageData);

            if (CurrentHealth <= 0)
                return;

            if (ShouldKeepCurrentCombatTarget())
                return;

            SetRetaliationTarget(
                damageData.Attacker);
        }

        private bool ShouldKeepCurrentCombatTarget()
        {
            if (graphAgent == null)
                return false;

            if (!graphAgent.GetVariable(
                    "TargetGameObject",
                    out BlackboardVariable<GameObject> targetVariable))
            {
                return false;
            }

            GameObject currentTarget =
                targetVariable.Value;

            if (currentTarget == null)
                return false;

            IDamageable targetDamageable =
                currentTarget.GetComponentInParent<IDamageable>();

            if (targetDamageable == null ||
                targetDamageable.CurrentHealth <= 0)
            {
                return false;
            }

            // Buildings never take priority over retaliation.
            if (currentTarget.GetComponentInParent<BaseBuilding>() != null)
            {
                return false;
            }

            if (UnitData == null ||
                UnitData.AttackConfig == null)
            {
                return false;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    targetDamageable.Transform.position);

            return distance <=
                UnitData.AttackConfig.Range;
        }

        private void SetRetaliationTarget(Transform attacker)
        {
            if (attacker == null)
                return;

            IDamageable attackerDamageable =
                attacker.GetComponentInParent<IDamageable>();

            if (attackerDamageable == null)
                return;

            if (!DamageRules.CanDamage(
                    Owner,
                    attackerDamageable.Owner))
            {
                return;
            }

            graphAgent.SetVariableValue(
                "RetaliationTarget",
                attackerDamageable.Transform.gameObject);
        }

#endregion

#region Save / Load
        public virtual void WriteToSaveData(UnitSaveData data)
        {
            data.InstanceID = InstanceID;

            if (UnitData is UnitSO unitSO)
                data.UnitTypeID = unitSO.SaveID;

            data.Position = transform.position;
            data.CurrentHealth = CurrentHealth;
            data.CurrentCommand = CurrentCommand;
        }

        public virtual void RestoreFromSave(UnitSaveData data)
        {
            SetHealth(data.CurrentHealth > 0 ? data.CurrentHealth : MaxHealth, MaxHealth);
            graphAgent.SetVariableValue("Command", data.CurrentCommand);
        }

        #endregion
    }
}