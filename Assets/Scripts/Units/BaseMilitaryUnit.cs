using ShiftedSignal.Garden.Combat;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class BaseMilitaryUnit : AbstractUnit
    {
        [SerializeField] private Transform projectileSpawnPoint;

        public override Transform ProjectileSpawnPoint => projectileSpawnPoint;
    }
}