using System;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.QuestSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.QuestSystem
{    
    [RequireComponent(typeof(SphereCollider))]
    public class QuestPoint : MonoBehaviour, IInteractable
    {
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
        public bool IsPlayerNear { get; private set; } = false;

        private void Awake()
        {
            questId = QuestInfoForPoint.ID;
            questIcon = GetComponentInChildren<QuestIcon>();
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Bus<QuestStateChangedEvent>.OnEvent += HandleQuestStateChanged;
        }

        private void OnDisable()
        {
            Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Player>() != null)
            {
                IsPlayerNear = true;
                Highlight(true);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<Player>() != null)
            {
                IsPlayerNear = true;
                Highlight(false);
            }
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
            if (!IsPlayerNear)
            {
                return;
            }

            if (!DialogueKnotName.Equals(""))
            {
                Bus<EnterDialogueEvent>.Raise(new EnterDialogueEvent(DialogueKnotName));
            }
            else
            {
                if (currentQuestState.Equals(QuestState.CAN_START) && StartPoint)
                {
                    Bus<StartQuestEvent>.Raise(new StartQuestEvent(questId));
                }
                else if (currentQuestState.Equals(QuestState.CAN_FINISH) && FinishPoint)
                {
                    Bus<FinishQuestEvent>.Raise(new FinishQuestEvent(questId));
                }
            }

            
        }

        public void Highlight(bool highlight)
        {
            if (highlight)
                sr.material.SetColor("_OuterOutlineColor", HighlightColor);
            else 
                sr.material.SetColor("_OuterOutlineColor", Color.black);
        }

        bool IInteractable.IsPlayerNear() => IsPlayerNear;
    }
}
