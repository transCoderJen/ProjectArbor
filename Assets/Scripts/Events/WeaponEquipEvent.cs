using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Events
{
    public struct WeaponEquipEvent : IEvent
    {
        public ItemData_Equipment Weapon { get; private set; }

        public WeaponEquipEvent(ItemData_Equipment weapon)
        {
            if (weapon.EquipmentType != EquipmentType.Weapon)
            {
                throw new System.ArgumentException("Item must be of equipment type Tool", nameof(weapon));
            }
            Weapon = weapon;
        }
    }
    
}