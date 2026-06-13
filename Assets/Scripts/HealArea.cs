using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.EntitySpace.PlantSpace;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

public class HealArea : MonoBehaviour
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

    private float effectTimer;

    private Collider[] plantHits = new Collider[50];
    private Collider[] playerHits = new Collider[5];

    public bool CanTriggerEffect =>
            effectTimer <= 0f &&
            (plantsInRange.Count > 0 || playersInRange.Count > 0);

    public IReadOnlyList<IPlant> PlantsInRange => plantsInRange;
    public IReadOnlyList<Enemy> EnemiesInRange => enemiesInRange;
    public IReadOnlyList<Player> PlayersInRange => playersInRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("ReturnToPool", 7f);
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    // Update is called once per frame
    private void Update()
    {
        effectTimer -= Time.deltaTime;

        ScanForTargets();

        if (CanTriggerEffect)
            TriggerEffect();
    }

    public void ScanForTargets()
    {
        plantsInRange.Clear();
        playersInRange.Clear();

        // Scan plants
        int plantCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            ScanRange,
            plantHits,
            PlantLayer);

        for (int i = 0; i < plantCount; i++)
        {
            Collider hit = plantHits[i];
            if (hit.gameObject == gameObject)
                continue;

            if (hit.TryGetComponent(out IPlant plant))
                plantsInRange.Add(plant);
        }

        // Scan players
        int playerCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            ScanRange,
            playerHits,
            PlayerLayer);

        for (int i = 0; i < playerCount; i++)
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
            
            foreach (IPlant plant in plantsInRange)
            {
                ApplyEffect(plant);
            }

            foreach (Player player in playersInRange)
            {
                ApplyEffect(player);
            }
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
}
