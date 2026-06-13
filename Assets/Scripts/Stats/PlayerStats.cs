using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    public class PlayerStats : CharacterHealth
    {
        private Player player;

        [Header("Combat")]
        [SerializeField] private int baseAttackDamage = 1;

        public int AttackDamage => baseAttackDamage;

        protected override void Awake()
        {
            base.Awake();
            player = GetComponent<Player>();
        }

        protected override void Die()
        {
            base.Die();

            player.Die();

            PlayerManager.Instance.Currency = 0;
        }
    }
}