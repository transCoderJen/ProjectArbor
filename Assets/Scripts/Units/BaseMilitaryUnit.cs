using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class BaseMilitaryUnit : AbstractUnit
    {
        [SerializeField]
        private Transform projectileSpawnPoint;

        [Header("Combat")]
        [SerializeField]
        private float pursuitLeashDistance = 15f;

        public override Transform ProjectileSpawnPoint =>
            projectileSpawnPoint;

        protected override bool AutoAcquireNearbyTargets =>
            true;

        public float PursuitLeashDistance =>
            pursuitLeashDistance;

        public bool HasDefenseAssignment
        {
            get
            {
                if (graphAgent == null)
                    return false;

                if (!graphAgent.GetVariable(
                        "HasDefenseAssignment",
                        out BlackboardVariable<bool> variable))
                {
                    return false;
                }

                return variable.Value;
            }
        }
        
        public GameObject DefenseBuilding
        {
            get
            {
                if (graphAgent == null)
                    return null;

                if (!graphAgent.GetVariable(
                        "DefenseBuilding",
                        out BlackboardVariable<GameObject> variable))
                {
                    return null;
                }

                return variable.Value;
            }
        }

        public GameObject DefenseTarget
        {
            get
            {
                if (graphAgent == null)
                    return null;

                if (!graphAgent.GetVariable(
                        "DefenseTarget",
                        out BlackboardVariable<GameObject> variable))
                {
                    return null;
                }

                return variable.Value;
            }
        }

        public GameObject ActiveBattleTarget
        {
            get
            {
                GameObject retaliationTarget =
                    GetBlackboardGameObject("RetaliationTarget");

                if (IsLivingTarget(retaliationTarget))
                    return retaliationTarget;

                if (IsLivingTarget(DefenseTarget))
                    return DefenseTarget;

                if (IsLivingTarget(CurrentTarget))
                    return CurrentTarget;

                return null;
            }
        }

        private GameObject GetBlackboardGameObject(string variableName)
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

        private bool IsLivingTarget(GameObject target)
        {
            if (target == null)
                return false;

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            return damageable != null &&
                damageable.CurrentHealth > 0;
        }

        protected override void Start()
        {
            base.Start();

            MilitaryDefenseManager.Instance?
                .RegisterMilitaryUnit(this);
        }

        protected override void OnDestroy()
        {
            MilitaryDefenseManager.Instance?
                .UnregisterMilitaryUnit(this);

            base.OnDestroy();
        }

        public void AssignBuildingDefense(
            BaseBuilding building,
            IDamageable attacker)
        {
            if (building == null ||
                graphAgent == null)
            {
                return;
            }

            graphAgent.SetVariableValue(
                "DefenseBuilding",
                building.gameObject);

            graphAgent.SetVariableValue(
                "DefenseTarget",
                attacker != null
                    ? attacker.Transform.gameObject
                    : null);

            graphAgent.SetVariableValue(
                "HasDefenseAssignment",
                true);

            Debug.Log(
                $"[DEFENSE ASSIGNMENT] {name} assigned to defend {building.name} | Target={attacker?.Transform?.name ?? "NULL"}");
        }

        public void UpdateDefenseTarget(
            IDamageable attacker)
        {
            if (graphAgent == null)
                return;

            graphAgent.SetVariableValue(
                "DefenseTarget",
                attacker != null
                    ? attacker.Transform.gameObject
                    : null);
        }

        public void CompleteDefenseAssignment()
        {
            Debug.Log(
                $"[DEFENSE CLEANUP] {name} completing defense assignment | " +
                $"Building={(DefenseBuilding != null ? DefenseBuilding.name : "NULL")} | " +
                $"Target={(DefenseTarget != null ? DefenseTarget.name : "NULL")}");

            BaseBuilding building = null;

            if (DefenseBuilding != null)
            {
                building =
                    DefenseBuilding
                        .GetComponentInParent<BaseBuilding>();
            }

            if (building != null &&
                MilitaryDefenseManager.Instance != null)
            {
                MilitaryDefenseManager.Instance
                    .ClearBuildingAssignment(building);

                return;
            }

            ClearBuildingDefense();
        }

        public void ClearBuildingDefense()
        {
            if (graphAgent == null)
                return;

            graphAgent.SetVariableValue<GameObject>(
                "DefenseBuilding",
                null);

            graphAgent.SetVariableValue<GameObject>(
                "DefenseTarget",
                null);

            graphAgent.SetVariableValue(
                "HasDefenseAssignment",
                false);

            Debug.Log(
                $"[DEFENSE ASSIGNMENT] {name} cleared defense assignment.");
        }       
    }
}