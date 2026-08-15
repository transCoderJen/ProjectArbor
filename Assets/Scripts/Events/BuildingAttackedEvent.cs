using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;
using UnityEngine;

namespace ShiftedSignal.Garden.Events
{
    public struct BuildingAttackedEvent : IEvent
    {
        public BaseBuilding Building;
        public Transform Attacker;

        public BuildingAttackedEvent(
            BaseBuilding building,
            Transform attacker)
        {
            Building = building;
            Attacker = attacker;
        }
    }
}