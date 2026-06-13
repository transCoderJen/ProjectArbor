using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    public class EnemyStats : CharacterHealth
    {
        private Enemy enemy;

        protected override void Awake()
        {
            base.Awake();
            enemy = GetComponent<Enemy>();
        }

        protected override void Die()
        {
            base.Die();

            enemy.Die();

            // TODO: Drop items
            // TODO: Give player reward
        }
    }
}