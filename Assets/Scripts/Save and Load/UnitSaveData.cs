using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.SaveAndLoad
{
    [System.Serializable]
    public class UnitSaveData
    {
        public string InstanceID;
        public string UnitTypeID;
        public Vector3 Position;
        public int CurrentHealth;
        public UnitCommands CurrentCommand;
    }
}