using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.Units;
using UnityEngine;

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
        }

        private void OnDisable()
        {
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawned;
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
            data.units.Clear();

            foreach (AbstractUnit unit in activeUnits)
            {
                if (unit == null)
                    continue;

                if (unit.UnitData is not UnitSO unitSO)
                    continue;

                data.units.Add(new UnitSaveData
                {
                    InstanceID = unit.InstanceID,
                    UnitTypeID = unitSO.SaveID,
                    Position = unit.transform.position,
                    CurrentHealth = unit.CurrentHealth
                });
            }
        }

        public void LoadData(GameData data)
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
                unit.RestoreFromSave(savedUnit.CurrentHealth);
            }
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
    }
}