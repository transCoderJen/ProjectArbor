using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.EntitySpace.PlantSpace;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Tools;
using UnityEngine;

namespace Assets.Scripts.Entity.Plant
{
    /// <summary>
    /// Stationary AOE support plant that heals nearby plants and players on a cooldown.
    /// </summary>
    public class HeartRoot : MonoBehaviour, IAOEPlant
    {
        [Header("AOE Heal")]
        [SerializeField] private float ScanRange = 6f;
        [SerializeField] private float EffectCooldown = 3f;
        [SerializeField] private int HealAmount = 10;

        [Header("Detection")]
        [SerializeField] private LayerMask PlantLayer;
        [SerializeField] private LayerMask PlayerLayer;

        private readonly List<IPlant> plantsInRange = new();
        private readonly List<Enemy> enemiesInRange = new();
        private readonly List<Player> playersInRange = new();

        private readonly Collider[] plantHits = new Collider[32];
        private readonly Collider[] playerHits = new Collider[32];

        private float effectTimer;

        public Transform Transform => transform;

        float IAOEPlant.ScanRange => ScanRange;
        float IAOEPlant.EffectCooldown => EffectCooldown;

        public bool AffectPlayers => true;
        public bool AffectEnemies => false;

        public bool CanTriggerEffect =>
            effectTimer <= 0f &&
            (plantsInRange.Count > 0 || playersInRange.Count > 0);

        public IReadOnlyList<IPlant> PlantsInRange => plantsInRange;
        public IReadOnlyList<Enemy> EnemiesInRange => enemiesInRange;
        public IReadOnlyList<Player> PlayersInRange => playersInRange;

        private void Awake()
        {
            InitializeAOEPlant();
        }

        private void Update()
        {
            effectTimer -= Time.deltaTime;

            ScanForTargets();

            if (CanTriggerEffect)
                TriggerEffect();
        }

        public void InitializeAOEPlant()
        {
            effectTimer = EffectCooldown;

            plantsInRange.Clear();
            enemiesInRange.Clear();
            playersInRange.Clear();
        }

        public void ScanForTargets()
        {
            plantsInRange.Clear();
            playersInRange.Clear();

            // Scan plants
            int plantHitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                ScanRange,
                plantHits,
                PlantLayer);

            for (int i = 0; i < plantHitCount; i++)
            {
                Collider hit = plantHits[i];
                if (hit.gameObject == gameObject)
                    continue;

                if (hit.TryGetComponent(out IPlant plant))
                    plantsInRange.Add(plant);
            }

            // Scan players
            int playerHitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                ScanRange,
                playerHits,
                PlayerLayer);

            for (int i = 0; i < playerHitCount; i++)
            {
                Collider hit = playerHits[i];
                if (hit.TryGetComponent(out Player player))
                    playersInRange.Add(player);
            }
        }

        public void TriggerEffect()
        {
            if (plantsInRange.Count == 0 && playersInRange.Count == 0)
            {
                return;
            }

            effectTimer = EffectCooldown;
            

            ObjectPoolManager.SpawnObject(PooledObjectList.HealArea, transform.position, Quaternion.identity, scale: 1.2f);

            return;

            // foreach (IPlant plant in plantsInRange)
            // {
            //     ApplyEffect(plant);
            // }

            // foreach (Player player in playersInRange)
            // {
            //     ApplyEffect(player);
            // }
        }

        public void ApplyEffect(IPlant plant)
        {
            if (plant == null)
                return;

            if (plant is IHealable healablePlant)
                healablePlant.Heal(HealAmount);
        }

        public void ApplyEffect(Player player)
        {
            if (player == null)
                return;

            if (player.TryGetComponent(out IHealable healable))
            {
                Debug.Log("Healing player");
                healable.Heal(HealAmount);
            }
        }

        public void ApplyEffect(Enemy enemy)
        {
            // HeartRoot does not affect enemies.
        }

        public void OnPlantDamaged(float damageAmount)
        {
            // Add damage reaction later if needed.
        }

        public void OnPlantDeath()
        {
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, ScanRange);
        }
    }
}