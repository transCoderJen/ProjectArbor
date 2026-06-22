using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class AbstractUnit : AbstractCommandable, IMoveable
    {
        [Header("Unit Config")]
        [SerializeField] private AbstractUnitSO UnitSO;

        protected override AbstractUnitSO Config => UnitSO;

        public float AgentRadius => agent.radius;

        private NavMeshAgent agent;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();

            Bus<UnitSpawnEvent>.Raise(new UnitSpawnEvent(this));
        }

#region Move / Stop

        public virtual void MoveTo(Vector3 position)
        {
            if (agent == null)
                return;

            agent.isStopped = false;
            agent.SetDestination(position);
        }

        public virtual void Stop()
        {
            if (agent == null)
                return;

            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

#endregion
    }
}