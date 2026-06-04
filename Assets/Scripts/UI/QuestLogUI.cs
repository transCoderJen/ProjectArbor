using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShiftedSignal.Garden.QuestSystem;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using System;
using UnityEngine.EventSystems;
using System.Reflection;

public class QuestLogUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject ContentParent;
    [SerializeField] private GameObject ButtonContentParent;
    [SerializeField] private QuestLogScrollingList ScrollingList;
    [SerializeField] private TextMeshProUGUI QuestDisplayNameText;
    [SerializeField] private TextMeshProUGUI QuestStatusText;
    [SerializeField] private TextMeshProUGUI GoldRewardsText;
    [SerializeField] private TextMeshProUGUI ExperienceRewardsText;
    [SerializeField] private TextMeshProUGUI MaterialRewardsText;
    [SerializeField] private TextMeshProUGUI LevelRequirmentsText;
    [SerializeField] private TextMeshProUGUI QuestRequirementsText;

    private Button firstSelectedButton;

    private void OnEnable()
    {
        Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        Bus<QuestStateChangedEvent>.OnEvent += HandleQuestStateChanged;

        RefreshQuestLog();
        // // Select the first quest and populate the info panel.
        // if (firstSelectedButton != null)
        // {
        //     firstSelectedButton.Select();

        //     QuestLogButton button =
        //         firstSelectedButton.GetComponent<QuestLogButton>();

        //     if (button != null)
        //     {
        //         SetQuestLogInfo(button.Quest);
        //     }
        // }
    }


    private void HandleQuestStateChanged(QuestStateChangedEvent evt)
    {
        QuestLogButton questLogButton = ScrollingList.CreateButtonIfNotExists(evt.Quest, () =>
        {
            SetQuestLogInfo(evt.Quest);
        });

        // initialize the first selected button if not already so that it's always at the top button
        if (firstSelectedButton == null)
        {
            firstSelectedButton = questLogButton.button;
            firstSelectedButton.Select();
        }


        questLogButton.SetState(evt.Quest.State);
        questLogButton.button.Select();
    }

    private void SetQuestLogInfo(Quest quest)
    {
        // Quest Name
        QuestDisplayNameText.text = quest.Info.DisplayName;

        // Status
        QuestStatusText.text = quest.GetFullStatusText();

        // Requirements
        LevelRequirmentsText.text = "Level " + quest.Info.LevelRequirement;
        QuestRequirementsText.text = "";
        foreach (QuestInfoSO prerequisiteQuestInfo in quest.Info.QuestPrerequisites)
        {
            QuestRequirementsText.text += prerequisiteQuestInfo.DisplayName + "\n";
        }

        // Rewards
        GoldRewardsText.text = quest.Info.GoldReward + " Gold";
        ExperienceRewardsText.text = quest.Info.ExperienceReward + " XP";
        MaterialRewardsText.text = "";
        if (quest.Info.ItemRewards == null)
            return;
        foreach (ItemReward reward in quest.Info.ItemRewards)
        {
            MaterialRewardsText.text += reward.Amount + reward.Data.ItemName;
        }
    }

    public void RefreshQuestLog()
    {
        if (QuestManager.Instance == null || ScrollingList == null)
        {
            return;
        }

        firstSelectedButton = null;

        foreach (Quest quest in QuestManager.Instance.GetReceivedQuests())
        {
            QuestLogButton questLogButton = ScrollingList.CreateButtonIfNotExists(quest, () =>
            {
                SetQuestLogInfo(quest);
            });

            questLogButton.SetState(quest.State);

            if (firstSelectedButton == null)
            {
                firstSelectedButton = questLogButton.button;
            }
        }

        if (firstSelectedButton != null)
        {
            firstSelectedButton.Select();

            QuestLogButton button = firstSelectedButton.GetComponent<QuestLogButton>();

            if (button != null)
            {
                SetQuestLogInfo(button.Quest);
            }
        }
    }
}
