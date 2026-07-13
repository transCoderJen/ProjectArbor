using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
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

        public float AgentRadius => agent.radius;
        private NavMeshAgent agent;
        private Animator anim;

        protected BehaviorGraphAgent graphAgent;

        private float lastAnimX;
        private float lastAnimY;
        private float nextAnimationUpdateTime;
        

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
            graphAgent.SetVariableValue("Command", UnitCommands.Stop);
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
                DameagableSensor.SetupFrom(UnitSO.AttackConfig);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Application.isPlaying)
                Bus<UnitDeathEvent>.Raise(new UnitDeathEvent(this));
            
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;

            if (DameagableSensor != null)
            {
                DameagableSensor.OnUnitEnter -= HandleUnitEnter;
                DameagableSensor.OnUnitExit -= HandleUnitExit;
            }
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (!graphAgent.GetVariable("TargetGameObject", out BlackboardVariable<GameObject> targetVariable))
                return;

            if (targetVariable.Value == null)
                return;

            if (evt.Unit == null || evt.Unit.gameObject != targetVariable.Value)
                return;

            List<GameObject> nearbyEnemies = SetNearbyEnemiesOnBlackboard();
            GameObject nextTarget = GetNextNonBuildingTarget(nearbyEnemies);

            if (nextTarget != null)
            {
                graphAgent.SetVariableValue("TargetGameObject", nextTarget);
                graphAgent.SetVariableValue("Command", UnitCommands.Attack);
                return;
            }

            graphAgent.SetVariableValue<GameObject>("TargetGameObject", null);

            if (IsAttackMoveActive())
            {
                return;
            }

            graphAgent.SetVariableValue("Command", UnitCommands.Stop);
        }

        protected virtual void Update()
        {
            UpdateMovementAnimation();
        }

        private void HandleUnitEnter(IDamageable damageable)
        {
            Debug.Log($"{name} sensor ENTER: {damageable}");

            bool attackMove = IsAttackMoveActive();
            Debug.Log($"{name} IsAttackMove: {attackMove}");

            List<GameObject> nearbyEnemies = SetNearbyEnemiesOnBlackboard();

            Debug.Log($"{name} nearby enemies count: {nearbyEnemies.Count}");

            if (!attackMove)
                return;

            if (graphAgent.GetVariable("TargetGameObject", out BlackboardVariable<GameObject> targetVariable))
            {
                Debug.Log($"{name} current target: {targetVariable.Value}");

                if (targetVariable.Value == null)
                {
                    GameObject nextTarget = GetNextNonBuildingTarget(nearbyEnemies);
                    Debug.Log($"{name} next target: {nextTarget}");

                    if (nextTarget != null)
                        graphAgent.SetVariableValue("TargetGameObject", nextTarget);
                }
            }
            else
            {
                Debug.LogWarning($"{name} could not find TargetGameObject blackboard variable.");
            }
        }

        private void HandleUnitExit(IDamageable damageable)
        {
            if (!IsAttackMoveActive())
                return;

            GameObject exitingTarget = null;
            Vector3 lastTargetPosition = transform.position;

            if (damageable is UnityEngine.Object unityObject && unityObject != null)
            {
                exitingTarget = damageable.Transform.gameObject;
                lastTargetPosition = damageable.Transform.position;
            }

            List<GameObject> nearbyEnemies = SetNearbyEnemiesOnBlackboard();

            if (!graphAgent.GetVariable("TargetGameObject", out BlackboardVariable<GameObject> targetVariable)
                || exitingTarget == null
                || exitingTarget != targetVariable.Value)
            {
                return;
            }

            GameObject nextTarget = GetNextNonBuildingTarget(nearbyEnemies);

            if (nextTarget != null)
            {
                graphAgent.SetVariableValue("TargetGameObject", nextTarget);
            }
            else
            {
                graphAgent.SetVariableValue<GameObject>("TargetGameObject", null);
                graphAgent.SetVariableValue("TargetLocation", lastTargetPosition);
            }
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

                Debug.Log($"{name} checking target: {target}");

                if (target == null)
                {
                    Debug.Log("Skipped target: null");
                    continue;
                }

                if (target is UnityEngine.Object unityObject && unityObject == null)
                {
                    Debug.Log("Skipped target: destroyed Unity object");
                    continue;
                }

                if (target.Owner == Owner)
                {
                    Debug.Log($"Skipped target: same team {target.Owner}");
                    continue;
                }

                GameObject targetObject = target.Transform.gameObject;
                Debug.Log($"Added enemy target: {targetObject.name}, team: {target.Owner}");

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

#region Attack
        public void Attack(IDamageable damageable)
        {
            if (agent != null)
                agent.isStopped = false;

            graphAgent.SetVariableValue("IsAttackMove", false);
            graphAgent.SetVariableValue("TargetGameObject", damageable.Transform.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Attack);
        }

        public void Attack(Vector3 location)
        {
            if (agent != null)
                agent.isStopped = false;

            Debug.Log($"{name} ATTACK MOVE to {location}");

            graphAgent.SetVariableValue("IsAttackMove", true);
            graphAgent.SetVariableValue<GameObject>("TargetGameObject", null);
            graphAgent.SetVariableValue("TargetLocation", location);
            graphAgent.SetVariableValue("Command", UnitCommands.Attack);
        }

        private bool IsAttackMoveActive()
        {
            return graphAgent.GetVariable("IsAttackMove", out BlackboardVariable<bool> variable)
                && variable.Value;
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