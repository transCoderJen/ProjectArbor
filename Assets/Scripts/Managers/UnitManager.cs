using System.Collections.Generic;
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

        private readonly List<AbstractUnit> activeUnits = new();

        private void OnEnable()
        {
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawned;
            Bus<UnitDestroyedEvent>.OnEvent += HandleUnitDestroyed;
            Bus<BuildingPlacedForConstructionEvent>.OnEvent += HandleBuildingPlacedForConstruction;
        }

        private void OnDisable()
        {
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawned;
            Bus<UnitDestroyedEvent>.OnEvent -= HandleUnitDestroyed;
            Bus<BuildingPlacedForConstructionEvent>.OnEvent -= HandleBuildingPlacedForConstruction;
        }

        private void HandleBuildingPlacedForConstruction(BuildingPlacedForConstructionEvent evt)
        {
            if (evt.Building == null)
                return;

            foreach (AbstractUnit unit in activeUnits)
            {
                if (unit is not Worker worker)
                    continue;

                if (!evt.Building.TryAssignBuilder(worker))
                    return;

                worker.Build(evt.Building);
            }
        }

        private void HandleUnitDestroyed(UnitDestroyedEvent evt)
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
            {
                activeUnits.Add(evt.Unit);
            }
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
            
            if (data.units == null || data.units.Count == 0)
            {
                LoadProfiler.End("UnitManager.LoadData", watch);
                return;
            }

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

            LoadProfiler.End("UnitManager.LoadData", watch);
        }

        private void ClearExistingUnits()
        {
            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                if (activeUnits[i] != null)
                {
                    Destroy(activeUnits[i].gameObject);
                }
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
        // private void OnValidate()
        // {
        //     FillUnitDatabase();
        // }

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