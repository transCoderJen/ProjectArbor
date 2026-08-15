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

        [SerializeReference] public BlackboardVariable<GameObject> RetaliationTarget;
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
            if (!HasValidUnitInputs())
                return Status.Failure;

            // The target disappeared before this node started.
            // Treat that as the attack being complete.
            if (Target == null ||
                Target.Value == null)
            {
                return Status.Success;
            }

            IDamageable damageable =
                Target.Value.GetComponentInParent<IDamageable>();

            if (damageable == null ||
                damageable.CurrentHealth <= 0)
            {
                return Status.Success;
            }

            attacker =
                Self.Value.GetComponent<IAttacker>();

            selfTransform =
                Self.Value.transform;

            navMeshAgent =
                selfTransform.GetComponent<NavMeshAgent>();

            unit =
                selfTransform.GetComponent<AbstractUnit>();

            animator =
                selfTransform.GetComponent<Animator>();

            selfDamageable =
                Self.Value.GetComponentInParent<IDamageable>();

            targetTransform =
                Target.Value.transform;

            targetDamageable =
                damageable;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (targetTransform == null ||
                targetDamageable == null ||
                targetDamageable.CurrentHealth <= 0)
            {
                return Status.Success;
            }

            if (ShouldYieldToRetaliation())
                return Status.Failure;
        
            animator?.SetFloat(AnimationConstants.SPEED, navMeshAgent.velocity.magnitude);

            if (Vector3.Distance(targetTransform.position, selfTransform.position) >= AttackConfig.Value.Range)
            {
                navMeshAgent.SetDestination(targetTransform.position);
                navMeshAgent.isStopped = false;
                animator?.SetBool(AnimationConstants.ATTACK, false);
                return Status.Running;
            }

            navMeshAgent.isStopped = true;

            Vector3 direction =targetTransform.position -selfTransform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(
                    (targetTransform.position - selfTransform.position).normalized,
                    Vector3.up
                );

                unit.SetRotation(lookRotation);
            }

            animator?.SetBool(AnimationConstants.ATTACK, true);

            if (Time.time >= lastAttackTime + AttackConfig.Value.Delay)
            {
                lastAttackTime = Time.time;

                

                if (AttackConfig.Value.Type == AttackType.Melee)
                {
                    DamageData damageData = CreateDamageData();
                    targetDamageable.TakeDamage(damageData);
                }
                else if (AttackConfig.Value.Type == AttackType.Projectile)
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
                AttackConfig.Value.Damage,
                selfDamageable != null ? selfDamageable.Owner : Owner.Unowned,
                selfTransform,
                AttackConfig.Value.Knockback,
                AttackConfig.Value.IgnoreFriendlyFire,
                AttackConfig.Value.CanDamageBuildables);
        }

        private bool HasValidUnitInputs()
    {
        return Self != null &&
            Self.Value != null &&
            Self.Value.TryGetComponent(out AbstractUnit _) &&
            Self.Value.TryGetComponent(out NavMeshAgent _) &&
            AttackConfig != null &&
            AttackConfig.Value != null;
    }

        private bool ShouldYieldToRetaliation()
        {
            if (RetaliationTarget == null ||
                RetaliationTarget.Value == null)
            {
                return false;
            }

            // We are already attacking the retaliation target,
            // so there is no reason to interrupt this action.
            if (Target.Value == RetaliationTarget.Value)
                return false;

            IDamageable retaliationDamageable =
                RetaliationTarget.Value
                    .GetComponentInParent<IDamageable>();

            if (retaliationDamageable == null ||
                retaliationDamageable.CurrentHealth <= 0)
            {
                return false;
            }

            return true;
        }
    }
}