using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(NavMeshAgent), typeof(BehaviorGraphAgent))]
    public abstract class AbstractUnit : AbstractCommandable, IMoveable
    {
        [Header("Unit Config")]
        protected override AbstractUnitSO Config => UnitSO;
        public AbstractUnitSO UnitData => UnitSO;
        [SerializeField] private AbstractUnitSO UnitSO;
        [SerializeField] private float AnimationUpdateRate = 0.1f;
        [SerializeField] private string instanceID;

        public string InstanceID => instanceID;

        public void SetInstanceID(string id)
        {
            instanceID = id;
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

        }

        protected override void Start()
        {
            base.Start();
            Bus<UnitSpawnEvent>.Raise(new UnitSpawnEvent(this));
            MoveTo(transform.position);
        }

        protected virtual void Update()
        {
            UpdateMovementAnimation();
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

#region Load From Save
        public virtual void RestoreFromSave(int savedHealth)
        {
            SetHealth(savedHealth > 0 ? savedHealth : MaxHealth, MaxHealth);
            Stop();
        }
#endregion
    }
}