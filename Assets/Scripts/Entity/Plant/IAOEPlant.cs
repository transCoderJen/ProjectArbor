using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlantSpace
{
    public interface IAOEPlant
    {
        Transform Transform { get; }

        float ScanRange { get; }

        float EffectCooldown { get; }

        bool AffectPlayers { get; }

        bool AffectEnemies { get; }

        bool CanTriggerEffect { get; }

        IReadOnlyList<Enemy> EnemiesInRange { get; }

        IReadOnlyList<Player> PlayersInRange { get; }

        /// <summary>
        /// Initializes the AOE plant.
        /// </summary>
        void InitializeAOEPlant();

        void ScanForTargets();

        void TriggerEffect();

        void ApplyEffect(Enemy enemy);

        void ApplyEffect(Player player);

        void OnPlantDamaged(float damageAmount);

        void OnPlantDeath();
    }
}