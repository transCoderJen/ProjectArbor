using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShiftedSignal.Garden.QuestSystem;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;

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
    [SerializeField] private Button SetActiveQuestButton;

    private Quest selectedQuest;
    private Button firstSelectedButton;

    private void OnEnable()
    {
        Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        Bus<QuestStateChangedEvent>.OnEvent += HandleQuestStateChanged;

        if (SetActiveQuestButton != null)
        {
            SetActiveQuestButton.onClick.RemoveListener(SetSelectedQuestAsActive);
            SetActiveQuestButton.onClick.AddListener(SetSelectedQuestAsActive);
        }

        RefreshQuestLog();
    }

    private void OnDisable()
    {
        Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        
        if (SetActiveQuestButton != null)
        {
            SetActiveQuestButton.onClick.RemoveListener(SetSelectedQuestAsActive);
        }
    }

    private void SelectQuest(Quest quest)
    {
        if (quest == null)
            return;

        selectedQuest = quest;
        SetQuestLogInfo(quest);

        if (SetActiveQuestButton != null)
        {
            SetActiveQuestButton.interactable =
                quest.IsReceived && quest.State != QuestState.FINISHED;
        }
    }

    private void SetSelectedQuestAsActive()
    {
        if (selectedQuest == null)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.SetTrackedQuest(selectedQuest);
    }

    private void HandleQuestStateChanged(QuestStateChangedEvent evt)
    {
        QuestLogButton questLogButton = ScrollingList.CreateButtonIfNotExists(evt.Quest, () =>
        {
            SelectQuest(evt.Quest);
        });

        if (firstSelectedButton == null)
        {
            firstSelectedButton = questLogButton.button;
        }

        questLogButton.SetState(evt.Quest.State);
    }

    private void SetQuestLogInfo(Quest quest)
    {
        if (quest == null)
            return;

        QuestDisplayNameText.text = quest.Info.DisplayName;
        QuestStatusText.text = quest.GetFullStatusText();

        LevelRequirmentsText.text = "Level " + quest.Info.LevelRequirement;

        QuestRequirementsText.text = "";

        foreach (QuestInfoSO prerequisiteQuestInfo in quest.Info.QuestPrerequisites)
        {
            QuestRequirementsText.text += prerequisiteQuestInfo.DisplayName + "\n";
        }

        GoldRewardsText.text = quest.Info.GoldReward + " Gold";
        ExperienceRewardsText.text = quest.Info.ExperienceReward + " XP";

        MaterialRewardsText.text = "";

        if (quest.Info.ItemRewards == null)
            return;

        foreach (ItemReward reward in quest.Info.ItemRewards)
        {
            MaterialRewardsText.text += reward.Amount + " " + reward.Data.ItemName + "\n";
        }
    }

    public void RefreshQuestLog()
    {
        if (QuestManager.Instance == null || ScrollingList == null)
            return;

        firstSelectedButton = null;

        foreach (Quest quest in QuestManager.Instance.GetReceivedQuests())
        {
            QuestLogButton questLogButton = ScrollingList.CreateButtonIfNotExists(quest, () =>
            {
                SelectQuest(quest);
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
                SelectQuest(button.Quest);
            }
        }
    }
}