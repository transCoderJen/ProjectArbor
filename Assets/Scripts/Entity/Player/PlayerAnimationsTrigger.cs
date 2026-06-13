using System.Collections;
using UnityEngine;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Buildable;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerAnimationsTriggers : MonoBehaviour
    {
        private Player player;
        private readonly Collider[] enemyHits = new Collider[50];

        private void Awake()
        {
            player = GetComponentInParent<Player>();
        }

        private void AnimationTrigger()
        {
            if (player == null)
                return;

            player.AnimationTrigger();
        }

        private void AttackTrigger()
        {
            if (player == null || player.AttackCheck == null)
                return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                player.AttackCheck.position,
                player.AttackCheckRadius,
                enemyHits);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = enemyHits[i];

                TryDamageEnemy(hit);
                TryDamageBuildable(hit);
            }
        }

        private void TryDamageEnemy(Collider hit)
        {
            EnemyStats enemyStats = hit.GetComponentInParent<EnemyStats>();

            if (enemyStats == null)
                return;

            StartCoroutine(SlowDownTime());

            enemyStats.TakeDamage(
                player.AttackDamage,
                true,
                player.transform);
        }

        private void TryDamageBuildable(Collider hit)
        {
            BaseBuildable buildable =
                hit.GetComponentInParent<BaseBuildable>();

            if (buildable == null)
                return;

            buildable.DoDamage(1);
        }

        private IEnumerator SlowDownTime()
        {
            Time.timeScale = 0.5f;

            yield return Helpers.GetWait(0.1f);

            Time.timeScale = 1f;
        }
    }
}