using UnityEngine;

namespace ShiftedSignal.Garden.Combat
{
    

    public readonly struct DamageData
    {
        public readonly int Amount;
        public readonly Transform Attacker;
        public readonly Owner Owner;

        public readonly bool Knockback;

        public readonly bool IgnoreFriendlyFire;
        public readonly bool CanDamageBuildables;

        public DamageData(
            int amount,
            Owner owner,
            Transform attacker = null,
            bool knockback = true,
            bool ignoreFriendlyFire = true,
            bool canDamageBuildables = true)
        {
            Amount = Mathf.Max(1, amount);

            Owner = owner;
            Attacker = attacker;

            Knockback = knockback;

            IgnoreFriendlyFire = ignoreFriendlyFire;
            CanDamageBuildables = canDamageBuildables;
        }
    }
}