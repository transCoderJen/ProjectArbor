using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.Stats;
using ShiftedSignalGames.GOF.ItemsAndInventory;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Tools;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{    
    public class PlayerAnimationsTriggers : MonoBehaviour
    {
        private Player player => GetComponentInParent<Player>();
        private Collider[] enemyHits = new Collider[50];

        private void AnimationTrigger()
        {
            player.AnimationTrigger();
        }

        private void AttackTrigger()
        {
            if (Debugging.Instance.DisableAttackDamage) return;
            // // AudioManager.instance.PlaySFX(SFXSounds.attack3, null);
        
            int enemyCount = Physics.OverlapSphereNonAlloc(
                player.AttackCheck.position, 
                player.AttackCheckRadius,
                enemyHits);

            for (int i = 0; i < enemyCount; i++)
            {
                Collider hit = enemyHits[i];
                
                Enemy enemy = hit.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                // This works for child colliders too

                // Vector3 closestPoint = hit.ClosestPoint(player.transform.position);

                // ObjectPoolManager.SpawnObject(
                //     player.EquippedWeapon.HitFX,
                //     closestPoint + new Vector3(0f, player.CheckHeight, 0f),
                //     Quaternion.LookRotation(player.FacingDir) * Quaternion.Euler(
                //         Random.Range(-15f, 15f),
                //         Random.Range(-30f, 30f),
                //         Random.Range(-15f, 15f)),
                //     parent: enemy.transform,
                //     scale: Random.Range(1f, 2.5f));
                    
                    StartCoroutine(nameof(SlowDownTime));
                }
                
                EnemyStats _target = hit.GetComponentInParent<EnemyStats>();

                if (_target != null)
                {
                    player.Stats.DoDamage(_target, Knockback: true);

                    if (Inventory.Instance.GetEquipment(EquipmentType.Weapon) == null)
                    {
                        Debug.Log("Inventory Get Equipment is returning null");
                    }
                    Inventory.Instance.GetEquipment(EquipmentType.Weapon)?.Effect(_target.transform);
                }
            }
        }
        

        private void ThrowSword()
        {       
            // SkillManager.instance.sword.CanUseSkill();
        }

        private IEnumerator SlowDownTime()
        {
            Time.timeScale = .5f;
            yield return Helpers.GetWait(.1f);
            Time.timeScale = 1f;
        }

    }
}
