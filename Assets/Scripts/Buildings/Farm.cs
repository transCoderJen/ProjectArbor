using UnityEngine;

namespace ShiftedSignal.Garden.Buildings
{
    public class Farm : Building
    {
        [Header("Farm")]
        [SerializeField] private bool triggerGameOverOnDeath = true;

        protected override void Awake()
        {
            base.Awake();

            priority = 100;
        }

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