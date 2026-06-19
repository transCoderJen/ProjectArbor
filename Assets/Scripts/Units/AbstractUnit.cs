using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class AbstractUnit : MonoBehaviour, IMoveable, ISelectable
    {
        public float AgentRadius => agent.radius;
        [SerializeField] private DecalProjector decalProjector;
        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            Bus<UnitSpawnEvent>.Raise(new UnitSpawnEvent(this));
        }

        #region Select / Deselect
        public void Select()
        {
            if (decalProjector != null)
            {
                decalProjector.gameObject.SetActive(true);
            }

            Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }

        public void Deselect()
        {
            if (decalProjector != null)
            {
                decalProjector.gameObject.SetActive(false);
            }

            Bus<UnitDeselectedEvent>.Raise(new UnitDeselectedEvent(this));
        }
#endregion

#region Move / Stop
        public void MoveTo(Vector3 position)
        {
            agent.SetDestination(position);
        }

        public void Stop()
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
#endregion


    }
}