using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using UnityEngine.AI;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Managers;
using NUnit.Framework;
using ShiftedSignal.Garden.Effects;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Attack Target", story: "[Self] attacks [Target] until it dies", category: "Action", id: "fd6074666e63e9208499e7e762f68931")]
    public partial class AttackTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Self;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<AttackConfigSO> AttackConfig;

        private NavMeshAgent navMeshAgent;
        private AbstractUnit unit;
        private Transform selfTransform;
        private Animator animator;

        private IDamageable selfDamageable;
        private IDamageable targetDamageable;
        private Transform targetTransform;

        private IAttacker attacker;

        private float lastAttackTime;

        protected override Status OnStart()
        {
            if (!HasValidInputs())
                return Status.Failure;
            
            attacker = Self.Value.GetComponent<IAttacker>();

            selfTransform = Self.Value.transform;
            navMeshAgent = selfTransform.GetComponent<NavMeshAgent>();
            unit = selfTransform.GetComponent<AbstractUnit>();
            animator = selfTransform.GetComponent<Animator>();

            selfDamageable = Self.Value.GetComponentInParent<IDamageable>();

            targetTransform = Target.Value.transform;
            targetDamageable = Target.Value.GetComponentInParent<IDamageable>();

            

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (targetDamageable == null || targetDamageable.CurrentHealth <= 0) 
                return Status.Success;
            
            animator?.SetFloat(AnimationConstants.SPEED, navMeshAgent.velocity.magnitude);

            if (Vector3.Distance(targetTransform.position, selfTransform.position) >= AttackConfig.Value.AttackRange)
            {
                navMeshAgent.SetDestination(targetTransform.position);
                navMeshAgent.isStopped = false;
                animator?.SetBool(AnimationConstants.ATTACK, false);
                return Status.Running;
            }

            navMeshAgent.isStopped = true;
            Quaternion lookRotation = Quaternion.LookRotation(
                (targetTransform.position - selfTransform.position).normalized,
                Vector3.up
            );

            unit.SetRotation(lookRotation);

            animator?.SetBool(AnimationConstants.ATTACK, true);

            if (Time.time >= lastAttackTime + AttackConfig.Value.AttackDelay)
            {
                lastAttackTime = Time.time;

                

                if (AttackConfig.Value.AttackType == AttackType.Melee)
                {
                    DamageData damageData = CreateDamageData();
                    targetDamageable.TakeDamage(damageData);
                }
                else if (AttackConfig.Value.AttackType == AttackType.Projectile)
                {
                    FireProjectile();
                }

            }

            

            return Status.Running;
        }

        protected override void OnEnd()
        {
            animator?.SetBool(AnimationConstants.ATTACK, false);

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                navMeshAgent.isStopped = false;
        }

        private void FireProjectile()
        {
            Transform spawnPoint = attacker.ProjectileSpawnPoint;

            Vector3 direction = targetDamageable.TargetPoint - spawnPoint.position;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

            GameObject projectileObject = ObjectPoolManager.SpawnObject(
                AttackConfig.Value.ProjectileType,
                spawnPoint.position,
                rotation,
                null,
                1);

            if (projectileObject == null)
                return;

            if (projectileObject.TryGetComponent(out Projectile projectile))
            {
                projectile.Initialize(
                    AttackConfig.Value,
                    target: Target.Value,
                    targetPointOverride: targetDamageable.TargetPoint);

                projectile.SetOwner(Self.Value);
                projectile.SetDamageData(CreateDamageData());
            }
        }

        private DamageData CreateDamageData()
        {
            return new DamageData(
                AttackConfig.Value.AttackDamage,
                selfDamageable != null ? selfDamageable.Owner : Owner.Unowned,
                selfTransform,
                AttackConfig.Value.Knockback,
                AttackConfig.Value.IgnoreFriendlyFire,
                AttackConfig.Value.CanDamageBuildables);
        }

        private bool HasValidInputs()
        {
            return Self.Value != null
                && Self.Value.TryGetComponent(out AbstractUnit _)
                && Self.Value.TryGetComponent(out NavMeshAgent _)
                && Target.Value != null
                && Target.Value.GetComponentInParent<IDamageable>() != null
                && AttackConfig.Value != null;
        }
    }
}