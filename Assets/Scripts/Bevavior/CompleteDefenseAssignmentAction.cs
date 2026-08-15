using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Complete Defense Assignment", story: "[Self] completes defense assignment and looks for another battle", category: "Action/Units", id: "bd603ead6b4e452ab4c0621ba6c96878")]
    public partial class CompleteDefenseAssignmentAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Self;

        protected override Status OnStart()
        {
            Debug.Log("This has started");
            if (Self == null ||
                Self.Value == null)
            {
                return Status.Failure;
            }

            BaseMilitaryUnit militaryUnit =
                Self.Value.GetComponent<BaseMilitaryUnit>();

            if (militaryUnit == null)
                return Status.Failure;

            militaryUnit.CompleteDefenseAssignment();

            return Status.Success;
        }
    }
}

