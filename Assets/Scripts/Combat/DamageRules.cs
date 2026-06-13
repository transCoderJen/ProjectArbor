namespace ShiftedSignal.Garden.Combat
{
    public static class DamageRules
    {
        public static bool CanDamage(CombatTeam attacker, CombatTeam target)
        {
            if (attacker == target)
                return false;

            switch (attacker)
            {
                case CombatTeam.Player:
                    return target == CombatTeam.Enemy ||
                           target == CombatTeam.Neutral;

                case CombatTeam.Enemy:
                    return target == CombatTeam.Player ||
                           target == CombatTeam.Friendly ||
                           target == CombatTeam.Buildable;

                case CombatTeam.Friendly:
                    return target == CombatTeam.Enemy;

                case CombatTeam.Buildable:
                    return target == CombatTeam.Enemy;

                case CombatTeam.Neutral:
                default:
                    return false;
            }
        }
    }
}