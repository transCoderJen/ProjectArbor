using UnityEngine;

namespace ShiftedSignal.Garden.Combat
{
    public static class DamageRules
    {
        public static bool CanDamage(CombatTeam attacker, CombatTeam target)
        {
            bool sameTeam = attacker == target;

            if (sameTeam)
                return false;

            switch (attacker)
            {
                case CombatTeam.Player:
                    return target == CombatTeam.Enemy ||
                        target == CombatTeam.Neutral;

                case CombatTeam.Enemy:
                    return target == CombatTeam.Player ||
                        target == CombatTeam.Friendly ||
                        target == CombatTeam.Buildable ||
                        target == CombatTeam.Neutral;

                case CombatTeam.Friendly:
                    return target == CombatTeam.Enemy ||
                        target == CombatTeam.Neutral;

                case CombatTeam.Buildable:
                    return target == CombatTeam.Enemy ||
                        target == CombatTeam.Neutral;

                case CombatTeam.Neutral:
                    return target != CombatTeam.Neutral;

                default:
                    return false;
            }
        }
    }
}