using System.Collections;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShiftedSignal.Garden.Raids
{
    public class NightRaidSpawner : MonoBehaviour
    {
        private enum RaidType
        {
            Animal,
            Villager,
            Mixed
        }

        private enum SpawnDirection
        {
            North,
            South,
            East,
            West
        }

        [Header("Spawn Points")]
        [SerializeField] private Transform northSpawn;
        [SerializeField] private Transform southSpawn;
        [SerializeField] private Transform eastSpawn;
        [SerializeField] private Transform westSpawn;

        [Header("Performance")]
        [SerializeField] private int maxActiveRaidEnemies = 25;
        [SerializeField] private float activeEnemyCheckDelay = 1f;

        [Header("Pools")]
        [SerializeField] private PooledObjectList animalPool = PooledObjectList.WolfEnemy;
        [SerializeField] private PooledObjectList villagerPool = PooledObjectList.VillagerEnemy;

        [Header("Base Difficulty")]
        [SerializeField] private int baseEnemyCount = 2;
        [SerializeField] private int enemiesPerNight = 1;
        [SerializeField] private float baseSpawnDelay = 4f;
        [SerializeField] private float minSpawnDelay = 0.75f;

        [Header("Corruption Scaling")]
        [SerializeField, Range(0, 100)] private int corruption;
        [SerializeField] private float corruptionDifficultyMultiplier = 0.08f;
        [SerializeField] private int animalRaidCorruptionThreshold = 25;

        [Header("Village Reputation Scaling")]
        [SerializeField, Range(-100, 100)] private int villageReputation;
        [SerializeField] private float negativeReputationDifficultyMultiplier = 0.06f;
        [SerializeField] private int villagerRaidReputationThreshold = -25;

        [Header("Mixed Raid")]
        [SerializeField] private int mixedRaidCorruptionThreshold = 60;
        [SerializeField] private int mixedRaidReputationThreshold = -60;
        [SerializeField, Range(0f, 1f)] private float villagerChanceInMixedRaid = 0.4f;

        [Header("Debug Raid")]
        [SerializeField] private bool debugRaidActive;
        [SerializeField] private int debugNightNumber = 1;

        private bool previousDebugRaidActive;
        private Coroutine raidRoutine;

        private int northSpawnCount;
        private int southSpawnCount;
        private int eastSpawnCount;
        private int westSpawnCount;

        private bool IsRaidRunning => raidRoutine != null;

        private void OnEnable()
        {
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
        }

        private void OnDisable()
        {
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
            StopNightRaid();
        }

        private void HandleNightStarted(NightStartedEvent evt)
        {
            StartNightRaid(
                evt.NightNumber,
                VillageManager.Instance.VillageReputation,
                CorruptionManager.Instance.Corruption
            );
        }

        private void Update()
        {
            if (debugRaidActive == previousDebugRaidActive)
                return;

            previousDebugRaidActive = debugRaidActive;

            if (debugRaidActive)
            {
                Debug.Log("Debug raid started.", this);
                StartNightRaid(debugNightNumber, villageReputation, corruption, true);
            }
            else
            {
                Debug.Log("Debug raid stopped.", this);
                StopNightRaid();
                LogSpawnCounts();
            }
        }

        public void StartNightRaid(
            int nightNumber,
            int currentVillageReputation,
            int currentCorruption,
            bool loopUntilStopped = false)
        {
            villageReputation = Mathf.Clamp(currentVillageReputation, -100, 100);
            corruption = Mathf.Clamp(currentCorruption, 0, 100);

            ResetSpawnCounts();

            if (raidRoutine != null)
                StopCoroutine(raidRoutine);

            raidRoutine = StartCoroutine(RaidRoutine(nightNumber, loopUntilStopped));
        }

        public void StopNightRaid()
        {
            if (raidRoutine == null)
                return;

            StopCoroutine(raidRoutine);
            raidRoutine = null;
        }

        public void DebugStartRaid(int difficulty, int reputation, int corruption)
        {
            debugNightNumber = Mathf.Max(1, difficulty);
            villageReputation = Mathf.Clamp(reputation, -100, 100);
            this.corruption = Mathf.Clamp(corruption, 0, 100);

            debugRaidActive = true;
            previousDebugRaidActive = true;

            Debug.Log(
                $"Debug raid started manually | Difficulty/Night: {debugNightNumber}, Reputation: {villageReputation}, Corruption: {this.corruption}",
                this
            );

            StartNightRaid(
                debugNightNumber,
                villageReputation,
                this.corruption,
                true
            );
        }

        public void DebugStopRaid()
        {
            debugRaidActive = false;
            previousDebugRaidActive = false;

            Debug.Log("Debug raid stopped manually.", this);

            StopNightRaid();
            LogSpawnCounts();
        }

        private IEnumerator RaidRoutine(int nightNumber, bool loopUntilStopped)
        {
            RaidType raidType = ChooseRaidType();

            int difficulty = CalculateDifficulty(nightNumber);
            int enemyCount = CalculateEnemyCount(difficulty);
            float spawnDelay = CalculateSpawnDelay(difficulty);

            Debug.Log(
                $"Raid Started | Type: {raidType} | Difficulty: {difficulty} | Enemy Count Per Wave: {enemyCount} | Delay: {spawnDelay} | Max Active: {maxActiveRaidEnemies}",
                this
            );

            do
            {
                for (int i = 0; i < enemyCount; i++)
                {
                    while (GetActiveRaidEnemyCount() >= maxActiveRaidEnemies)
                    {
                        yield return new WaitForSeconds(activeEnemyCheckDelay);

                        if (!IsRaidRunning)
                            yield break;
                    }

                    PooledObjectList selectedPool = ChoosePoolForRaid(raidType);
                    SpawnEnemy(selectedPool);

                    yield return new WaitForSeconds(spawnDelay);
                }

                LogSpawnCounts();

            } while (loopUntilStopped);

            raidRoutine = null;
        }

        private int GetActiveRaidEnemyCount()
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            return enemies.Length;
        }

        private RaidType ChooseRaidType()
        {
            bool hostileVillage = villageReputation <= mixedRaidReputationThreshold;
            bool heavyCorruption = corruption >= mixedRaidCorruptionThreshold;

            if (hostileVillage && heavyCorruption)
                return RaidType.Mixed;

            if (villageReputation <= villagerRaidReputationThreshold)
                return RaidType.Villager;

            return RaidType.Animal;
        }

        private int CalculateDifficulty(int nightNumber)
        {
            int difficulty = Mathf.Max(1, nightNumber);

            difficulty += Mathf.RoundToInt(corruption * corruptionDifficultyMultiplier);

            if (villageReputation < 0)
            {
                difficulty += Mathf.RoundToInt(
                    Mathf.Abs(villageReputation) * negativeReputationDifficultyMultiplier
                );
            }

            return difficulty;
        }

        private int CalculateEnemyCount(int difficulty)
        {
            return baseEnemyCount + difficulty * enemiesPerNight;
        }

        private float CalculateSpawnDelay(int difficulty)
        {
            float delay = baseSpawnDelay - difficulty * 0.25f;
            return Mathf.Max(minSpawnDelay, delay);
        }

        private PooledObjectList ChoosePoolForRaid(RaidType raidType)
        {
            return raidType switch
            {
                RaidType.Villager => villagerPool,

                RaidType.Mixed => Random.value <= villagerChanceInMixedRaid
                    ? villagerPool
                    : animalPool,

                _ => animalPool
            };
        }

        private void SpawnEnemy(PooledObjectList poolType)
        {
            Transform spawnPoint = GetRandomSpawnPoint(out SpawnDirection direction);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"No raid spawn point assigned for {direction}.", this);
                return;
            }

            GameObject enemyObject = ObjectPoolManager.SpawnObject(
                poolType,
                spawnPoint.position,
                spawnPoint.rotation
            );

            if (enemyObject == null)
                return;

            IRaidEnemy raidEnemy = enemyObject.GetComponent<IRaidEnemy>();

            if (raidEnemy == null)
            {
                Debug.LogWarning($"{enemyObject.name} does not implement IRaidEnemy.", enemyObject);
                return;
            }

            raidEnemy.StartRaid();

            AddSpawnCount(direction);

            Debug.Log(
                $"Spawned {poolType} from {direction}. Active: {GetActiveRaidEnemyCount()}/{maxActiveRaidEnemies} | Counts N:{northSpawnCount} S:{southSpawnCount} E:{eastSpawnCount} W:{westSpawnCount}",
                this
            );
        }

        private Transform GetRandomSpawnPoint(out SpawnDirection direction)
        {
            direction = (SpawnDirection)Random.Range(0, 4);

            return direction switch
            {
                SpawnDirection.North => northSpawn,
                SpawnDirection.South => southSpawn,
                SpawnDirection.East => eastSpawn,
                SpawnDirection.West => westSpawn,
                _ => southSpawn
            };
        }

        private void AddSpawnCount(SpawnDirection direction)
        {
            switch (direction)
            {
                case SpawnDirection.North:
                    northSpawnCount++;
                    break;

                case SpawnDirection.South:
                    southSpawnCount++;
                    break;

                case SpawnDirection.East:
                    eastSpawnCount++;
                    break;

                case SpawnDirection.West:
                    westSpawnCount++;
                    break;
            }
        }

        private void ResetSpawnCounts()
        {
            northSpawnCount = 0;
            southSpawnCount = 0;
            eastSpawnCount = 0;
            westSpawnCount = 0;
        }

        private void LogSpawnCounts()
        {
            Debug.Log(
                $"Raid Spawn Counts | North: {northSpawnCount}, South: {southSpawnCount}, East: {eastSpawnCount}, West: {westSpawnCount}",
                this
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawSpawnGizmo(northSpawn, Color.blue);
            DrawSpawnGizmo(southSpawn, Color.green);
            DrawSpawnGizmo(eastSpawn, Color.yellow);
            DrawSpawnGizmo(westSpawn, Color.red);
        }

        private void DrawSpawnGizmo(Transform spawnPoint, Color color)
        {
            if (spawnPoint == null)
                return;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(spawnPoint.position, 1f);
            Gizmos.DrawLine(
                spawnPoint.position,
                spawnPoint.position + spawnPoint.forward * 2f
            );
        }
#endif
    }
}