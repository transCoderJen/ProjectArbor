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

        public DamageData(
            int amount,
            CombatTeam attackerTeam,
            Transform attacker = null,
            bool knockback = true)
        {
            Amount = Mathf.Max(1, amount);
            AttackerTeam = attackerTeam;
            Attacker = attacker;
            Knockback = knockback;
        }
    }
}