using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.Units;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.Managers
{
    public class UnitManager : Singleton<UnitManager>, ISaveManager
    {
        [Header("Unit Database")]
        [SerializeField] private List<UnitSO> unitDatabase = new();

        [Header("Construction")]
        [SerializeField] private float ConstructionWorkerSearchRadius = 150f;

        private readonly List<AbstractUnit> activeUnits = new();
        private readonly List<BaseBuilding> activeConstructionSites = new();

        private void OnEnable()
        {
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawned;
            Bus<UnitDeathEvent>.OnEvent += HandleUnitDestroyed;
            Bus<BuildingPlacedForConstructionEvent>.OnEvent += HandleBuildingPlacedForConstruction;
        }

        private void OnDisable()
        {
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawned;
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDestroyed;
            Bus<BuildingPlacedForConstructionEvent>.OnEvent -= HandleBuildingPlacedForConstruction;
        }

        private void HandleBuildingPlacedForConstruction(BuildingPlacedForConstructionEvent evt)
        {
            if (evt.Building == null)
                return;

            RegisterConstructionSite(evt.Building);
            AssignNearbyWorkersToBuilding(evt.Building);
        }

        private void RegisterConstructionSite(BaseBuilding building)
        {
            if (building == null || !building.IsUnderConstruction)
                return;

            if (!activeConstructionSites.Contains(building))
                activeConstructionSites.Add(building);
        }

        private void AssignNearbyWorkersToBuilding(BaseBuilding building)
        {
            if (building == null || !building.IsUnderConstruction)
                return;

            List<Worker> availableWorkers = GetAvailableWorkersSortedByDistance(building);

            foreach (Worker worker in availableWorkers)
            {
                if (!TryAssignWorkerToBuilding(worker, building))
                    break;
            }
        }

        public bool TryAssignWorkerToBuilding(Worker worker, BaseBuilding building)
        {
            if (worker == null || building == null)
                return false;

            if (worker.HasBuildAssignment)
                return false;

            if (!building.IsUnderConstruction)
                return false;

            if (!building.TryAssignBuilder(worker))
                return false;

            worker.Build(building);
            return true;
        }

        public bool TryAssignWorkerToNextConstructionSite(Worker worker)
        {
            if (worker == null)
                return false;

            BaseBuilding building = FindAvailableConstructionSite(worker);

            if (building == null)
                return false;

            return TryAssignWorkerToBuilding(worker, building);
        }

        private BaseBuilding FindAvailableConstructionSite(Worker worker)
        {
            if (worker == null)
                return null;

            CleanupConstructionSites();

            BaseBuilding bestBuilding = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (BaseBuilding building in activeConstructionSites)
            {
                if (building == null)
                    continue;

                if (!building.IsUnderConstruction)
                    continue;

                if (!building.HasBuilderSlot)
                    continue;

                float distanceSqr =
                    (worker.transform.position - building.transform.position).sqrMagnitude;

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestBuilding = building;
                bestDistanceSqr = distanceSqr;
            }

            return bestBuilding;
        }

        private List<Worker> GetAvailableWorkersSortedByDistance(BaseBuilding building)
        {
            List<Worker> availableWorkers = new();

            if (building == null)
                return availableWorkers;

            float maxDistanceSqr =
                ConstructionWorkerSearchRadius * ConstructionWorkerSearchRadius;

            foreach (AbstractUnit unit in activeUnits)
            {
                if (unit is not Worker worker)
                    continue;

                if (worker.HasBuildAssignment)
                    continue;

                float distanceSqr =
                    (worker.transform.position - building.transform.position).sqrMagnitude;

                if (distanceSqr > maxDistanceSqr)
                    continue;

                availableWorkers.Add(worker);
            }

            availableWorkers.Sort((a, b) =>
            {
                float aDistance =
                    (a.transform.position - building.transform.position).sqrMagnitude;

                float bDistance =
                    (b.transform.position - building.transform.position).sqrMagnitude;

                return aDistance.CompareTo(bDistance);
            });

            return availableWorkers;
        }

        private void CleanupConstructionSites()
        {
            for (int i = activeConstructionSites.Count - 1; i >= 0; i--)
            {
                BaseBuilding building = activeConstructionSites[i];

                if (building == null || !building.IsUnderConstruction)
                    activeConstructionSites.RemoveAt(i);
            }
        }

        private void HandleUnitDestroyed(UnitDeathEvent evt)
        {
            if (evt.Unit == null)
                return;

            activeUnits.Remove(evt.Unit);
        }

        private void HandleUnitSpawned(UnitSpawnEvent evt)
        {
            if (evt.Unit == null)
                return;

            if (!activeUnits.Contains(evt.Unit))
                activeUnits.Add(evt.Unit);
        }

        public void SaveData(ref GameData data)
        {
            if (data.units == null)
                data.units = new List<UnitSaveData>();

            data.units.Clear();

            foreach (AbstractUnit unit in activeUnits)
            {
                if (unit == null)
                    continue;

                UnitSaveData unitSaveData = new UnitSaveData();
                unit.WriteToSaveData(unitSaveData);

                data.units.Add(unitSaveData);
            }
        }

        public void LoadData(GameData data)
        {
            var watch = LoadProfiler.Start("UnitManager.LoadData");

            try
            {
                if (data.units == null || data.units.Count == 0)
                    return;

                ClearExistingUnits();

                foreach (UnitSaveData savedUnit in data.units)
                {
                    UnitSO unitSO = FindUnitByTypeID(savedUnit.UnitTypeID);

                    if (unitSO == null || unitSO.Prefab == null)
                        continue;

                    GameObject unitObject = Instantiate(
                        unitSO.Prefab,
                        savedUnit.Position,
                        Quaternion.identity);

                    if (!unitObject.TryGetComponent(out AbstractUnit unit))
                        continue;

                    unit.SetInstanceID(savedUnit.InstanceID);
                    unit.RestoreFromSave(savedUnit);
                }
            }
            finally
            {
                LoadProfiler.End("UnitManager.LoadData", watch);
            }
        }

        private void ClearExistingUnits()
        {
            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                if (activeUnits[i] != null)
                    Destroy(activeUnits[i].gameObject);
            }

            activeUnits.Clear();
        }

        private UnitSO FindUnitByTypeID(string typeID)
        {
            foreach (UnitSO unitSO in unitDatabase)
            {
                if (unitSO == null)
                    continue;

                if (unitSO.SaveID == typeID)
                    return unitSO;
            }

            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Fill Unit Database")]
        private void FillUnitDatabase()
        {
            unitDatabase.Clear();

            const string rootFolder = "Assets/Prefabs/Units/Units";

            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                Debug.LogError($"Unit database folder not found: {rootFolder}", this);
                return;
            }

            string[] assetGuids =
                AssetDatabase.FindAssets(
                    "t:UnitSO",
                    new[] { rootFolder });

            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                UnitSO unit =
                    AssetDatabase.LoadAssetAtPath<UnitSO>(path);

                if (unit == null)
                    continue;

                if (!unitDatabase.Contains(unit))
                    unitDatabase.Add(unit);
            }

            Debug.Log($"Filled Unit Database. Count={unitDatabase.Count}", this);

            EditorUtility.SetDirty(this);
        }
#endif
    }
}