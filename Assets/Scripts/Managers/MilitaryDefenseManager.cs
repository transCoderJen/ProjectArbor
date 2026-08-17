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

        private readonly Dictionary<BaseBuilding, BuildingThreat>
            activeThreats = new();

        [SerializeField]
        private float threatReassessmentInterval = 1f;

        private float nextThreatReassessmentTime;


        #region Unity Lifecycle

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

        private void Update()
        {
            if (Time.time < nextThreatReassessmentTime)
                return;

            nextThreatReassessmentTime =
                Time.time + threatReassessmentInterval;

            ReassessThreats();
        }

        #endregion


        #region Registration

        public void RegisterMilitaryUnit(
            BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            militaryUnits.Add(unit);
        }

        public void UnregisterMilitaryUnit(
            BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            militaryUnits.Remove(unit);

            RemoveAssignmentsFor(unit);
        }

        #endregion


        #region Threat Tracking

        private void HandleBuildingAttacked(
            BuildingAttackedEvent evt)
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
            
            Debug.Log(
                $"[BUILDING ATTACK VALIDATION] " +
                $"Building={evt.Building.name} | " +
                $"BuildingOwner={evt.Building.Owner} | " +
                $"Attacker={attacker.Transform.name} | " +
                $"AttackerOwner={attacker.Owner}");
            
            if (attacker.Owner != Owner.Enemy)
            {
                Debug.LogWarning(
                    $"[BUILDING ATTACK REJECTED] " +
                    $"{attacker.Transform.name} ({attacker.Owner}) " +
                    $"was reported attacking {evt.Building.name}");

                return;
            }

            // Track the threat for this building.
            if (!activeThreats.TryGetValue(
                    evt.Building,
                    out BuildingThreat threat))
            {
                threat = new BuildingThreat
                {
                    Building = evt.Building
                };

                activeThreats.Add(
                    evt.Building,
                    threat);
            }

            CleanupInactiveAttackers(threat);

            threat.LastAttackTime = Time.time;
            
            threat.Attackers.Add(attacker);

            int threatScore =
                CalculateThreatScore(threat);

            int defenseScore =
                CalculateDefenseScore(threat);

            Debug.Log(
                $"[THREAT UPDATE] {threat.Building.name} | " +
                $"Attackers={threat.Attackers.Count} | " +
                $"ThreatScore={threatScore} | " +
                $"Defenders={threat.Defenders.Count} | " +
                $"DefenseScore={defenseScore}");

            TryUpdateExistingAssignments(
                evt.Building,
                attacker);

            ReinforceThreat(threat);
        }

        private void ReassessThreats()
        {
            List<BaseBuilding> threatsToRemove = new();

            foreach (
                KeyValuePair<BaseBuilding, BuildingThreat> pair
                in activeThreats)
            {
                BuildingThreat threat =
                    pair.Value;

                if (threat == null ||
                    threat.Building == null)
                {
                    threatsToRemove.Add(
                        pair.Key);

                    continue;
                }

                CleanupInactiveAttackers(
                    threat);

                if (threat.Attackers.Count == 0)
                {
                    ReleaseThreatDefenders(threat);

                    threatsToRemove.Add(
                        pair.Key);

                    continue;
                }

                int threatScore =
                    CalculateThreatScore(
                        threat);

                int defenseScore =
                    CalculateDefenseScore(threat);

                Debug.Log(
                    $"[THREAT] {threat.Building.name} | " +
                    $"Attackers={threat.Attackers.Count} | " +
                    $"Threat={threatScore} | " +
                    $"Defenders={threat.Defenders.Count} | " +
                    $"Defense={defenseScore}");
                            }

            foreach (BaseBuilding building
                     in threatsToRemove)
            {
                activeThreats.Remove(
                    building);

                Debug.Log(
                    $"[THREAT CLEARED] " +
                    $"{(building != null ? building.name : "Missing Building")}");
            }

            BuildingThreat priorityThreat =
                FindMostUnderDefendedThreat();

            if (priorityThreat != null)
            {
                int threatScore =
                    CalculateThreatScore(priorityThreat);

                int defenseScore =
                    CalculateDefenseScore(priorityThreat);

                Debug.Log(
                    $"[FORCE ALLOCATION] Highest need={priorityThreat.Building.name} | " +
                    $"Threat={threatScore} | " +
                    $"Defense={defenseScore} | " +
                    $"Deficit={threatScore - defenseScore}");

                ReinforceThreat(priorityThreat);
            }
        }

        private void CleanupInactiveAttackers(
            BuildingThreat threat)
        {
            if (threat == null ||
                threat.Building == null)
            {
                return;
            }

            threat.Attackers.RemoveWhere(attacker =>
            {
                if (attacker == null)
                    return true;

                if (attacker is not Component component ||
                    component == null)
                {
                    return true;
                }

                if (attacker.CurrentHealth <= 0)
                    return true;

                AbstractUnit unit =
                    component.GetComponentInParent<AbstractUnit>();

                if (unit == null)
                    return true;

                GameObject currentTarget =
                    unit.CurrentTarget;

                GameObject retaliationTarget =
                    unit.RetaliationTarget;

                /*
                * A temporary null target does not prove
                * that the attacker abandoned this threat.
                */
                if (currentTarget == null)
                    return false;

                /*
                * Retaliation is temporary.
                * Keep this attacker associated with the
                * building it was originally threatening.
                */
                if (retaliationTarget != null)
                {
                    IDamageable retaliationDamageable =
                        retaliationTarget
                            .GetComponentInParent<IDamageable>();

                    if (retaliationDamageable != null &&
                        retaliationDamageable.CurrentHealth > 0)
                    {
                        return false;
                    }
                }

                BaseBuilding targetedBuilding =
                    currentTarget
                        .GetComponentInParent<BaseBuilding>();

                return targetedBuilding !=
                    threat.Building;
            });
        }

        private void ReleaseThreatDefenders(
            BuildingThreat threat)
        {
            if (threat == null)
                return;

            foreach (BaseMilitaryUnit defender in threat.Defenders)
            {
                if (defender == null)
                    continue;

                defender.ClearBuildingDefense();

                Debug.Log(
                    $"[THREAT RELEASE] {defender.name} released from " +
                    $"{(threat.Building != null ? threat.Building.name : "Missing Building")}");
            }

            threat.Defenders.Clear();
        }

        #endregion


        #region Threat Scoring & Force Allocation

        private int GetPriorityValue(
            TargetPriority priority)
        {
            return priority switch
            {
                TargetPriority.Low => 1,
                TargetPriority.Medium => 2,
                TargetPriority.High => 3,
                TargetPriority.Highest => 4,
                _ => 1
            };
        }

        private int CalculateThreatScore(
            BuildingThreat threat)
        {
            if (threat == null ||
                threat.Building == null)
            {
                return 0;
            }

            int score =
                GetPriorityValue(
                    threat.Building.TargetPriority);

            foreach (IDamageable attacker in threat.Attackers)
            {
                if (attacker == null ||
                    attacker.CurrentHealth <= 0)
                {
                    continue;
                }

                if (attacker is not AbstractCommandable commandable)
                    continue;

                score +=
                    GetPriorityValue(
                        commandable.TargetPriority);
            }

            return score;
        }

        private int CalculateDefenseScore(
            BuildingThreat threat)
        {
            if (threat == null)
                return 0;

            int score = 0;

            foreach (BaseMilitaryUnit defender in threat.Defenders)
            {
                if (defender == null ||
                    defender.CurrentHealth <= 0)
                {
                    continue;
                }

                score += GetPriorityValue(
                    defender.TargetPriority);
            }

            return score;
        }

        private BuildingThreat FindMostUnderDefendedThreat()
        {
            BuildingThreat bestThreat = null;
            int largestDeficit = 0;

            foreach (BuildingThreat threat in activeThreats.Values)
            {
                if (threat == null ||
                    threat.Building == null)
                {
                    continue;
                }

                int threatScore =
                    CalculateThreatScore(threat);

                int defenseScore =
                    CalculateDefenseScore(threat);

                int deficit =
                    threatScore - defenseScore;

                // Already adequately defended.
                if (deficit <= 0)
                    continue;

                if (deficit <= largestDeficit)
                    continue;

                largestDeficit = deficit;
                bestThreat = threat;
            }

            return bestThreat;
        }

        private void ReinforceThreat(
            BuildingThreat threat)
        {
            if (threat == null ||
                threat.Building == null)
            {
                return;
            }

            int threatScore =
                CalculateThreatScore(threat);

            int defenseScore =
                CalculateDefenseScore(threat);

            while (defenseScore < threatScore)
            {
                BaseMilitaryUnit defender =
                    FindBestAvailableDefender(
                        threat.Building);

                if (defender == null)
                {
                    Debug.Log(
                        $"[FORCE ALLOCATION] {threat.Building.name} still under-defended | " +
                        $"Threat={threatScore} | " +
                        $"Defense={defenseScore} | " +
                        $"No available defenders");

                    return;
                }

                /*
                 * For now, choose one of the living attackers
                 * as this defender's DefenseTarget.
                 */
                IDamageable defenseTarget =
                    GetDefenseTarget(threat);

                if (defenseTarget == null)
                    return;

                AssignDefender(
                    defender,
                    threat.Building,
                    defenseTarget);

                /*
                 * AssignDefender() adds the unit to
                 * threat.Defenders, so recalculate.
                 */
                defenseScore =
                    CalculateDefenseScore(threat);

                Debug.Log(
                    $"[FORCE ALLOCATION] {threat.Building.name} | " +
                    $"Assigned={defender.name} | " +
                    $"Threat={threatScore} | " +
                    $"Defense={defenseScore}");
            }
        }

        private IDamageable GetDefenseTarget(
            BuildingThreat threat)
        {
            foreach (IDamageable attacker
                     in threat.Attackers)
            {
                if (attacker == null ||
                    attacker.CurrentHealth <= 0)
                {
                    continue;
                }

                return attacker;
            }

            return null;
        }

        #endregion


        #region Building Defense

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

            if (unit.CurrentHealth <= 0)
                return false;

            // Friendly military units can defend friendly/buildable structures.
            if (unit.Owner != Owner.Friendly)
                return false;

            if (building.Owner != Owner.Friendly &&
                building.Owner != Owner.Buildable)
            {
                return false;
            }

            if (unit.HasDefenseAssignment)
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

            if (activeThreats.TryGetValue(
                    building,
                    out BuildingThreat threat))
            {
                threat.Defenders.Add(unit);

                Debug.Log(
                    $"[THREAT DEFENDER] {building.name} | " +
                    $"Added={unit.name} | " +
                    $"Defenders={threat.Defenders.Count}");
            }

            unit.AssignBuildingDefense(
                building,
                attacker);

            Debug.Log(
                $"[DEFENSE] {unit.name} assigned to defend {building.name}");
        }

        private bool TryUpdateExistingAssignments(
            BaseBuilding building,
            IDamageable attacker)
        {
            if (building == null ||
                !activeThreats.TryGetValue(
                    building,
                    out BuildingThreat threat))
            {
                return false;
            }

            if (threat.Defenders.Count == 0)
                return false;

            bool updatedAny = false;

            foreach (BaseMilitaryUnit defender in threat.Defenders)
            {
                if (defender == null ||
                    defender.CurrentHealth <= 0)
                {
                    continue;
                }

                defender.UpdateDefenseTarget(
                    attacker);

                updatedAny = true;
            }

            return updatedAny;
        }

        public void ClearDefenderAssignment(
            BaseBuilding building,
            BaseMilitaryUnit defender)
        {
            if (building == null ||
                defender == null)
            {
                return;
            }

            if (activeThreats.TryGetValue(
                    building,
                    out BuildingThreat threat))
            {
                threat.Defenders.Remove(
                    defender);
            }

            defender.ClearBuildingDefense();
        }

        private void RemoveAssignmentsFor(
            BaseMilitaryUnit unit)
        {
            if (unit == null)
                return;

            foreach (BuildingThreat threat
                    in activeThreats.Values)
            {
                if (threat == null)
                    continue;

                threat.Defenders.Remove(
                    unit);
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


    #region Threat Data

    public class BuildingThreat
    {
        public BaseBuilding Building;

        public readonly HashSet<IDamageable> Attackers = new();
        public readonly HashSet<BaseMilitaryUnit> Defenders = new();

        public float LastAttackTime;
    }

    #endregion
}