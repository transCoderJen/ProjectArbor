using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Behavior;
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
        protected override AbstractUnitSO Config => UnitSO;
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

            if (DameagableSensor != null)
            {
                DameagableSensor.OnUnitEnter += HandleUnitEnterOrExit;
                DameagableSensor.OnUnitExit += HandleUnitEnterOrExit;
                DameagableSensor.SetupFrom(UnitSO.AttackConfig);
            }
        }

        protected virtual void OnDestroy()
        {
            Bus<UnitDeathEvent>.Raise(new UnitDeathEvent(this));
            if (DameagableSensor != null)
            {
                DameagableSensor.OnUnitEnter -= HandleUnitEnterOrExit;
                DameagableSensor.OnUnitExit -= HandleUnitEnterOrExit;
            }
        }

        protected virtual void Update()
        {
            UpdateMovementAnimation();
        }

        private void HandleUnitEnterOrExit(IDamageable damageable)
        {
            List<GameObject> nearbyEnemies = new();

            foreach (IDamageable target in DameagableSensor.Damageables)
            {
                if (target == null)
                    continue;

                if (target is UnityEngine.Object unityObject && unityObject == null)
                    continue;

                nearbyEnemies.Add(target.Transform.gameObject);
            }

            nearbyEnemies.Sort(new DamageableTargetGameObjectComparer(transform.position));

            graphAgent.SetVariableValue("NearbyEnemies", nearbyEnemies);
        }
#region Move / Stop

        public virtual void MoveTo(Vector3 position)
        {
            
            graphAgent.SetVariableValue("TargetLocation", position);
            graphAgent.SetVariableValue("Command", UnitCommands.Move);
        }

        public virtual void Stop()
        {
            graphAgent.SetVariableValue("Command", UnitCommands.Stop);
        }

        protected virtual void UpdateMovementAnimation()
        {
            if (anim == null || agent == null)
                return;
            
            // throttle this
            if (Time.time < nextAnimationUpdateTime)
                return;

            nextAnimationUpdateTime = Time.time + AnimationUpdateRate;

            Vector2 moveDirection = new Vector2(agent.velocity.x, agent.velocity.z);

            anim.SetFloat(AnimationConstants.SPEED, moveDirection.magnitude);

            if (moveDirection.sqrMagnitude < 0.01f)
                return;

            moveDirection.Normalize();

            float animX = Mathf.Abs(moveDirection.x) > 0.5f
                ? Mathf.Sign(moveDirection.x)
                : 0f;

            float animY = Mathf.Abs(moveDirection.y) > 0.5f
                ? Mathf.Sign(moveDirection.y)
                : 0f;

            if (animX == 0f && animY == 0f)
                return;

            if (animX != lastAnimX)
            {
                anim.SetFloat("MovementX", animX);
                lastAnimX = animX;
            }

            if (animY != lastAnimY)
            {
                anim.SetFloat("MovementY", animY);
                lastAnimY = animY;
            }
        }

#endregion

#region Attack
        public void Attack(IDamageable damageable)
        {
            graphAgent.SetVariableValue("TargetGameObject", damageable.Transform.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Attack);
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