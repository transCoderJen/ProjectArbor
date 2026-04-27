using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;

namespace ShiftedSignal.Garden.Events
{
    public struct WeaponQuickSelectEvent : IEvent
    {
        public ItemData_Equipment Weapon { get; private set; }

        public WeaponQuickSelectEvent(ItemData_Equipment weapon)
        {
            Debug.Log("Inside event call");
            if (weapon.EquipmentType != EquipmentType.Weapon)
            {
                throw new System.ArgumentException("Item must be of equipment type Tool", nameof(weapon));
            }
            Weapon = weapon;
        }
    }
    
}