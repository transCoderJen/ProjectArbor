using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class Farm : BaseBuilding
    {
        [Header("Farm")]
        [SerializeField] private bool triggerGameOverOnDeath = true;

        protected override void DestroyBuilding()
        {
            if (triggerGameOverOnDeath)
            {
                Debug.Log("Farm destroyed. Trigger game over here.", this);

                // Later:
                // GameManager.Instance.GameOver();
            }

            base.DestroyBuilding();
        }
    }
}