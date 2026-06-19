using UnityEngine;

namespace ShiftedSignal.Garden.Combat
{
    public enum CombatTeam
    {
        Neutral,
        Player,
        Enemy,
        Friendly,
        Buildable
    }

    public readonly struct DamageData
    {
        public readonly int Amount;
        public readonly Transform Attacker;
        public readonly CombatTeam AttackerTeam;

        public readonly bool Knockback;

        public readonly bool IgnoreFriendlyFire;
        public readonly bool CanDamageBuildables;

        public DamageData(
            int amount,
            CombatTeam attackerTeam,
            Transform attacker = null,
            bool knockback = true,
            bool ignoreFriendlyFire = true,
            bool canDamageBuildables = true)
        {
            Amount = Mathf.Max(1, amount);

            AttackerTeam = attackerTeam;
            Attacker = attacker;

            Knockback = knockback;

            IgnoreFriendlyFire = ignoreFriendlyFire;
            CanDamageBuildables = canDamageBuildables;
        }
    }
}