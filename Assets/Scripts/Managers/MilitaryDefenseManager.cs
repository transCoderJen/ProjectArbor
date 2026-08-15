using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public class MilitaryDefenseManager : MonoBehaviour
    {
        public static MilitaryDefenseManager Instance { get; private set; }

        private readonly HashSet<BaseMilitaryUnit> militaryUnits = new();

        private readonly Dictionary<BaseBuilding, BaseMilitaryUnit>
            buildingAssignments = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Bus<BuildingAttackedEvent>.OnEvent +=
                HandleBuildingAttacked;
        }

        private void OnDisable()
        {
            Bus<BuildingAttackedEvent>.OnEvent -=
                HandleBuildingAttacked;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Registration

        public void RegisterMilitaryUnit(BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            militaryUnits.Add(unit);
        }

        public void UnregisterMilitaryUnit(BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            militaryUnits.Remove(unit);

            RemoveAssignmentsFor(unit);
        }

        #endregion

        #region Building Defense

        private void HandleBuildingAttacked(BuildingAttackedEvent evt)
        {
            if (evt.Building == null ||
                evt.Attacker == null)
            {
                return;
            }

            IDamageable attacker =
                evt.Attacker.GetComponentInParent<IDamageable>();

            if (attacker == null)
                return;

            if (TryUpdateExistingAssignment(
                    evt.Building,
                    attacker))
            {
                return;
            }

            BaseMilitaryUnit defender =
                FindBestAvailableDefender(
                    evt.Building);

            if (defender == null)
                return;

            AssignDefender(
                defender,
                evt.Building,
                attacker);
        }

        private BaseMilitaryUnit FindBestAvailableDefender(
            BaseBuilding building)
        {
            BaseMilitaryUnit bestUnit = null;
            float closestDistanceSquared = float.MaxValue;

            foreach (BaseMilitaryUnit unit in militaryUnits)
            {
                if (!IsEligibleDefender(
                        unit,
                        building))
                {
                    continue;
                }

                float distanceSquared =
                    (
                        unit.transform.position -
                        building.transform.position
                    ).sqrMagnitude;

                if (distanceSquared >=
                    closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared =
                    distanceSquared;

                bestUnit = unit;
            }

            return bestUnit;
        }

        private bool IsEligibleDefender(
            BaseMilitaryUnit unit,
            BaseBuilding building)
        {
            if (unit == null ||
                building == null)
            {
                return false;
            }

            if (unit.Owner != building.Owner)
                return false;

            if (unit.CurrentHealth <= 0)
                return false;

            if (unit.HasDefenseAssignment)
                return false;

            if (unit.IsInCombat)
                return false;

            return true;
        }

        private void AssignDefender(
            BaseMilitaryUnit unit,
            BaseBuilding building,
            IDamageable attacker)
        {
            if (unit == null ||
                building == null)
            {
                return;
            }

            buildingAssignments[building] =
                unit;

            unit.AssignBuildingDefense(
                building,
                attacker);

            Debug.Log(
                $"[DEFENSE] {unit.name} assigned to defend {building.name}");
        }

        private bool TryUpdateExistingAssignment(
            BaseBuilding building,
            IDamageable attacker)
        {
            if (!buildingAssignments.TryGetValue(
                    building,
                    out BaseMilitaryUnit assignedUnit))
            {
                return false;
            }

            if (assignedUnit == null ||
                assignedUnit.CurrentHealth <= 0)
            {
                buildingAssignments.Remove(
                    building);

                return false;
            }

            assignedUnit.UpdateDefenseTarget(
                attacker);

            return true;
        }

        public void ClearBuildingAssignment(
            BaseBuilding building)
        {
            if (building == null)
                return;

            if (!buildingAssignments.TryGetValue(
                    building,
                    out BaseMilitaryUnit unit))
            {
                return;
            }

            buildingAssignments.Remove(
                building);

            if (unit != null)
                unit.ClearBuildingDefense();
        }

        private void RemoveAssignmentsFor(
            BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            List<BaseBuilding> buildingsToRemove =
                new();

            foreach (
                KeyValuePair<BaseBuilding, BaseMilitaryUnit>
                    assignment in buildingAssignments)
            {
                if (assignment.Value == unit)
                {
                    buildingsToRemove.Add(
                        assignment.Key);
                }
            }

            foreach (BaseBuilding building
                     in buildingsToRemove)
            {
                buildingAssignments.Remove(
                    building);
            }
        }

        #endregion

        #region Battle Assistance

        public bool TryFindBattleToAssist(
            BaseMilitaryUnit requestingUnit,
            out IDamageable assistTarget)
        {
            assistTarget = null;

            if (requestingUnit == null)
                return false;

            float closestDistanceSquared =
                float.MaxValue;

            foreach (BaseMilitaryUnit unit in militaryUnits)
            {
                if (unit == null ||
                    unit == requestingUnit)
                {
                    continue;
                }

                if (unit.Owner != requestingUnit.Owner)
                    continue;

                if (unit.CurrentHealth <= 0)
                    continue;

                GameObject battleTarget =
                    unit.ActiveBattleTarget;

                if (battleTarget == null)
                    continue;

                IDamageable damageable =
                    battleTarget.GetComponentInParent<IDamageable>();

                if (damageable == null ||
                    damageable.CurrentHealth <= 0)
                {
                    continue;
                }

                float distanceSquared =
                    (
                        requestingUnit.transform.position -
                        unit.transform.position
                    ).sqrMagnitude;

                if (distanceSquared >=
                    closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared =
                    distanceSquared;

                assistTarget =
                    damageable;
            }

            return assistTarget != null;
        }

        #endregion
    }
}