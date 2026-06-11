using System.Collections;
using UnityEngine;
using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Buildable;

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

                BaseBuildable buildable = hit.GetComponentInParent<BaseBuildable>();
                if (buildable != null)
                {
                    buildable.DoDamage(1);
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
