using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    [RequireComponent(typeof(SphereCollider))]
    public class QuestPoint : MonoBehaviour, IInteractable
    {
        private static readonly int OuterOutlineColorId = Shader.PropertyToID("_OuterOutlineColor");

        [Header("Dialogue")]
        [SerializeField] private string DialogueKnotName;

        [Header("Quest")]
        [SerializeField] private QuestInfoSO QuestInfoForPoint;

        [Header("Config")]
        [SerializeField] private bool StartPoint = true;
        [SerializeField] private bool FinishPoint = true;
        
        [ColorUsage(false, true)] 
        [SerializeField] private Color HighlightColor;

        private string questId;
        private QuestState currentQuestState;
        private QuestIcon questIcon;
        private SpriteRenderer sr;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            questId = QuestInfoForPoint.ID;
            questIcon = GetComponentInChildren<QuestIcon>();
            sr = GetComponentInChildren<SpriteRenderer>();
            propertyBlock = new MaterialPropertyBlock();

            SetOutlineColor(Color.black);
        }

        private void OnEnable()
        {
            Bus<QuestStateChangedEvent>.OnEvent += HandleQuestStateChanged;
        }

        private void OnDisable()
        {
            Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        }

        private void HandleQuestStateChanged(QuestStateChangedEvent evt)
        {
            if (evt.Quest.Info.ID.Equals(questId))
            {
                currentQuestState = evt.Quest.State;
                questIcon.SetState(currentQuestState, StartPoint, FinishPoint);
            }
        }

        public void Interact(Player player)
        {
            if (!string.IsNullOrEmpty(DialogueKnotName))
            {
                Bus<EnterDialogueEvent>.Raise(new EnterDialogueEvent(DialogueKnotName));
                return;
            }

            if (currentQuestState.Equals(QuestState.CAN_START) && StartPoint)
            {
                Bus<StartQuestEvent>.Raise(new StartQuestEvent(questId));
            }
            else if (currentQuestState.Equals(QuestState.CAN_FINISH) && FinishPoint)
            {
                Bus<FinishQuestEvent>.Raise(new FinishQuestEvent(questId));
            }
        }

        public void Highlight(bool highlight)
        {
            SetOutlineColor(highlight ? HighlightColor : Color.black);
        }

        private void SetOutlineColor(Color color)
        {
            if (sr == null)
                return;

            sr.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(OuterOutlineColorId, color);
            sr.SetPropertyBlock(propertyBlock);
        }
    }
}