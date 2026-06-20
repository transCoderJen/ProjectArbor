using ShiftedSignal.Garden.Combat;

namespace ShiftedSignal.Garden.Interfaces
{
    public interface IDamageable
    {
        CombatTeam Team { get; }
        virtual void TakeDamage(DamageData damageData)
        {
            
        }
    }
}