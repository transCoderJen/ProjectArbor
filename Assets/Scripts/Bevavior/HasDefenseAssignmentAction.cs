using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Has Defense Assignment", story: "Unit [hasDefenseAssignment] set to True", category: "Action/Conditional", id: "a4a8701e04789fbe3c74139aabe24e21")]
    public partial class HasDefenseAssignmentAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Self;

        [SerializeReference]
        public BlackboardVariable<bool> HasDefenseAssignment;

        [SerializeReference]
        public BlackboardVariable<GameObject> DefenseTarget;

        protected override Status OnStart()
        {
            if (Self == null ||
                Self.Value == null)
            {
                return Status.Failure;
            }

            if (HasDefenseAssignment == null ||
                !HasDefenseAssignment.Value)
            {
                return Status.Failure;
            }

            BaseMilitaryUnit militaryUnit =
                Self.Value.GetComponent<BaseMilitaryUnit>();

            if (militaryUnit == null)
                return Status.Failure;

            // Assignment exists, but its target has disappeared.
            if (DefenseTarget == null ||
                DefenseTarget.Value == null)
            {
                militaryUnit.CompleteDefenseAssignment();

                return Status.Failure;
            }

            IDamageable target =
                DefenseTarget.Value
                    .GetComponentInParent<IDamageable>();

            // Target is no longer a valid living threat.
            if (target == null ||
                target.CurrentHealth <= 0)
            {
                militaryUnit.CompleteDefenseAssignment();

                return Status.Failure;
            }

            return Status.Success;
        }
    }
}

