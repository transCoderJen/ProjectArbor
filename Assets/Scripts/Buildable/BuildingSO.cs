using System;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public enum BuildPlacementMode
    {
        GridOnly,
        Anywhere
    }

    [Serializable]
    public struct TowerStats
    {
        [Header("Targeting")]
        public float AttackRange;
        public float AttackCooldown;

        [Header("Projectile")]
        public float ProjectileSpeed;

        [Range(0f, 100f)]
        public float ProjectileAccuracy;

        public float ProjectileBuildUpTime;
        public bool ProjectileRotate;
        public float ProjectileRotateAmount;
        public bool ProjectileBounce;
        public float ProjectileBounceForce;
        public float ProjectileLifetime;
    }

    [CreateAssetMenu(fileName = "New Buildable Data", menuName = "Data/Buildable")]
    public class BuildingSO : UnitSO
    {
        [Header("Ghost")]
        public Material GhostMaterial;

        [Header("Placement Rules")]
        public BuildPlacementMode PlacementMode = BuildPlacementMode.GridOnly;
        public bool RequiresActiveGrowBlock = true;
        public float XRotation = 30f;

        [Header("Tower Stats")]
        public bool HasTowerStats;
        public TowerStats BaseTowerStats;
    }
}