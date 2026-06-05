using System.Collections;
using UnityEngine;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.Dialogue
{
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private string knotName;

        [Header("Trigger Settings")]
        [SerializeField] private bool triggerOnlyOnce = true;
        [SerializeField] private float triggerDelay = 0f;

        [Header("Player Facing")]
        [SerializeField] private bool facePlayerTowardThisObject = false;

        [Header("Quest Requirement")]
        [SerializeField] private bool requireQuestState = false;
        [SerializeField] private QuestInfoSO requiredQuest;
        [SerializeField] private QuestState requiredState;

        private bool isWaitingToTrigger;

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<Player>(out Player player))
                return;

            if (isWaitingToTrigger)
                return;

            if (!QuestRequirementPasses())
                return;

            if (facePlayerTowardThisObject)
            {
                FacePlayerTowardTrigger(player);
            }

            StartCoroutine(TriggerDialogueRoutine());
        }

        private IEnumerator TriggerDialogueRoutine()
        {
            isWaitingToTrigger = true;

            if (triggerDelay > 0f)
                yield return new WaitForSeconds(triggerDelay);

            Bus<EnterDialogueEvent>.Raise(
                new EnterDialogueEvent(knotName)
            );

            if (triggerOnlyOnce)
            {
                Destroy(gameObject);
            }
            else
            {
                isWaitingToTrigger = false;
            }
        }

        private bool QuestRequirementPasses()
        {
            if (!requireQuestState)
                return true;

            if (requiredQuest == null)
                return false;

            Quest quest = QuestManager.Instance.GetQuestById(requiredQuest.ID);

            return quest != null &&
                   quest.State == requiredState;
        }

        private void FacePlayerTowardTrigger(Player player)
        {
            Vector3 direction = transform.position - player.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                return;

            direction.Normalize();

            player.FacingDir = direction;
            player.LastFacingDir = direction;
        }
    }
}