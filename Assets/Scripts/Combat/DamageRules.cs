using UnityEngine;

namespace ShiftedSignal.Garden.Combat
{
    public static class DamageRules
    {
        public static bool CanDamage(Owner attacker, Owner target)
        {
            bool sameTeam = attacker == target;

            if (sameTeam)
                return false;

            switch (attacker)
            {
                case Owner.Player:
                    return target == Owner.Enemy ||
                        target == Owner.Unowned;

                case Owner.Enemy:
                    return target == Owner.Player ||
                        target == Owner.Friendly ||
                        target == Owner.Buildable ||
                        target == Owner.Unowned;

                case Owner.Friendly:
                    return target == Owner.Enemy ||
                        target == Owner.Unowned;

                case Owner.Buildable:
                    return target == Owner.Enemy ||
                        target == Owner.Unowned;

                case Owner.Unowned:
                    return target != Owner.Unowned;

                default:
                    return false;
            }
        }
    }
}