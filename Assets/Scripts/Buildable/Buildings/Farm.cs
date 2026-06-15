using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class Farm : BaseBuildable
    {
        [Header("Farm")]
        [SerializeField] private bool triggerGameOverOnDeath = true;

        protected override void Die()
        {
            base.Die();

            if (!triggerGameOverOnDeath)
                return;

            Debug.Log("Farm destroyed. Trigger game over here.", this);

            // Later:
            // GameManager.Instance.GameOver();
        }
    }
}