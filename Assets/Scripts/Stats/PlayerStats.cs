using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    
    public class PlayerStats : CharacterStats
    {
        private Player player;
        
        protected override void Start()
        {
            base.Start();

            player = GetComponent<Player>();
        }

        public override void TakeDamage(int _damage, bool _knockback, Transform attacker)
        {
            if (_damage >= player.Stats.GetMaxHealthValue() * .3f)
                _knockback = true;

            base.TakeDamage(_damage, _knockback, attacker);
        }

        protected override void Die()
        {
            base.Die();
            player.Die();

            // GameManager.instance.lostCurrencyAmount = PlayerManager.instance.currency;
            
            // PlayerManager.instance.currency = 0;

            // GetComponent<PlayerItemDrop>()?.GenerateDrop();
        }

        public override void DecreaseHealthBy(int _damage)
        {
            base.DecreaseHealthBy(_damage);
            // Inventory inventory = Inventory.Instance;
            // if (inventory.canUseArmor())
            // {
            //     ItemData_Equipment armor =inventory.GetEquipment(EquipmentType.Armor);
            //     if (armor != null)
            //         armor.Effect(player.transform);
            // }
        }

        public override void OnEvasion()
        {
            // TODO On Evasion Skill
        }
    }
}

