using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(NavMeshAgent), typeof(BehaviorGraphAgent))]
    public abstract class AbstractUnit : AbstractCommandable, IMoveable
    {
        [Header("Unit Config")]
        [SerializeField] private AbstractUnitSO UnitSO;

        protected override AbstractUnitSO Config => UnitSO;
        public float AgentRadius => agent.radius;
        private NavMeshAgent agent;
        protected BehaviorGraphAgent graphAgent;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            graphAgent = GetComponent<BehaviorGraphAgent>();
            graphAgent.SetVariableValue("Command", UnitCommands.Stop);
        }

        protected override void Start()
        {
            base.Start();
            Bus<UnitSpawnEvent>.Raise(new UnitSpawnEvent(this));
            MoveTo(transform.position);
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

#endregion
    }
}