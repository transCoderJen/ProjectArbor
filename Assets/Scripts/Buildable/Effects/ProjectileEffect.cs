using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    [CreateAssetMenu(fileName = "Attack Nearest Effect", menuName = "Data/Buildable Effects/Attack Nearest")]
    public class ProjectileEffect : BuildableEffect
    {
        [Header("Targeting")]
        [SerializeField] private float Range = 5f;
        [SerializeField] private LayerMask EnemyLayer;

        [Header("Attack")]
        [SerializeField] private float AttackCooldown = 1f;

        public override void Apply(BaseBuildable buildable)
        {
            Debug.Log($"[{name}] Apply() called");

            if (buildable == null)
            {
                Debug.LogWarning($"[{name}] Buildable is NULL");
                return;
            }

            Debug.Log($"[{name}] Buildable = {buildable.name}");

            if (!buildable.IsEffectReady(this, AttackCooldown))
            {
                Debug.Log($"[{name}] Effect on cooldown");
                return;
            }

            Debug.Log($"[{name}] Cooldown passed");

            Collider[] hits = Physics.OverlapSphere(
                buildable.transform.position,
                Range,
                EnemyLayer,
                QueryTriggerInteraction.Ignore);

            Debug.Log($"[{name}] Found {hits.Length} enemies in range");

            if (hits.Length == 0)
            {
                Debug.Log($"[{name}] No enemies detected");
                return;
            }

            foreach (Collider hit in hits)
            {
                Debug.Log($"[{name}] Hit: {hit.name}");
            }

            Transform target = GetNearestEnemy(buildable.transform.position, hits);

            if (target == null)
            {
                Debug.LogWarning($"[{name}] Target was null");
                return;
            }

            Debug.Log($"[{name}] Target selected: {target.name}");

            Vector3 direction = target.position - buildable.transform.position;
            direction.y = 0f;

            Debug.Log($"[{name}] Direction: {direction}");

            Quaternion rotation = Quaternion.LookRotation(direction);

            Debug.Log($"[{name}] Spawning projectile");

            GameObject projectile = ObjectPoolManager.SpawnObject(
                PooledObjectList.Bullet,
                buildable.transform.position,
                rotation,
                null,
                1);

            Debug.Log($"[{name}] Spawn result: {(projectile != null ? projectile.name : "NULL")}");

            buildable.MarkEffectUsed(this);

            Debug.Log($"[{name}] Attack complete");
        }

        private Transform GetNearestEnemy(Vector3 origin, Collider[] enemies)
        {
            Transform nearestEnemy = null;
            float nearestDistance = Mathf.Infinity;

            foreach (Collider enemy in enemies)
            {
                float distance = Vector3.Distance(origin, enemy.transform.position);

                Debug.Log($"Checking {enemy.name} : {distance}");

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }

            if (nearestEnemy != null)
            {
                Debug.Log($"Nearest enemy: {nearestEnemy.name} ({nearestDistance})");
            }

            return nearestEnemy;
        }
    }
}