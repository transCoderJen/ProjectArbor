using ShiftedSignal.Garden.Combat;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public class BaseMilitaryUnit : AbstractUnit
    {
        [SerializeField] private Transform projectileSpawnPoint;

        [Header("Combat")]
        [SerializeField] private float pursuitLeashDistance = 15f;

        public override Transform ProjectileSpawnPoint => projectileSpawnPoint;
        
        protected override bool AutoAcquireNearbyTargets => true;

        public float PursuitLeashDistance => pursuitLeashDistance; //TODO implement upon fleeing behavior of enemy.  
                                                                    // The unit should oinly pursue fleeing characters 
                                                                    // so far from their orignally stationed point
    }
}