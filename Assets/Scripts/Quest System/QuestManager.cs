using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.QuestSystem;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.Stats;
using UnityEngine;

public class QuestManager : Singleton<QuestManager>, ISaveManager
{
    [Header("Config")]
    [SerializeField] private bool LoadQuestData = true;

    public Quest TrackedQuest { get; private set; }

    private Dictionary<string, Quest> questMap;
    private int currentPlayerLevel = 1;

    protected override void Awake()
    {
        base.Awake();
        questMap = CreateQuestMap();
    }

    private void OnEnable()
    {
        Bus<StartQuestEvent>.OnEvent += StartQuest;
        Bus<AdvanceQuestEvent>.OnEvent += AdvanceQuest;
        Bus<FinishQuestEvent>.OnEvent += FinishQuest;
        Bus<PlayerLevelUpEvent>.OnEvent += HandlePlayerLevelUp;
        Bus<QuestStepStateChangedEvent>.OnEvent += HandleQuestStepStateChanged;
        Bus<QuestReceivedEvent>.OnEvent += RecieveQuest;
    }

    private void OnDisable()
    {
        Bus<StartQuestEvent>.OnEvent -= StartQuest;
        Bus<AdvanceQuestEvent>.OnEvent -= AdvanceQuest;
        Bus<FinishQuestEvent>.OnEvent -= FinishQuest;
        Bus<PlayerLevelUpEvent>.OnEvent -= HandlePlayerLevelUp;
        Bus<QuestStepStateChangedEvent>.OnEvent -= HandleQuestStepStateChanged;
        Bus<QuestReceivedEvent>.OnEvent -= RecieveQuest;
    }

    private void Update()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.State == QuestState.REQUIREMENTS_NOT_MET && CheckRequirementsMet(quest))
            {
                ChangeQuestState(quest.Info.ID, QuestState.CAN_START);
            }
        }
    }

    public void SetTrackedQuest(Quest quest)
    {
        if (quest == null)
            return;

        if (!quest.IsReceived)
            return;

        if (quest.State == QuestState.FINISHED)
            return;

        TrackedQuest = quest;

        Bus<TrackedQuestChangedEvent>.Raise(
            new TrackedQuestChangedEvent(quest));
    }

    public void SetTrackedQuest(string questId)
    {
        Quest quest = GetQuestById(questId);

        if (quest == null)
            return;

        SetTrackedQuest(quest);
    }

    private void RefreshQuestData()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.State == QuestState.IN_PROGRESS)
            {
                quest.InstantiateCurrentQuestStep(transform);
            }

            Bus<QuestStateChangedEvent>.Raise(new QuestStateChangedEvent(quest));
        }

        AutoTrackFirstReceivedQuest();
    }

    private void AutoTrackFirstReceivedQuest()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.IsReceived && quest.State == QuestState.IN_PROGRESS)
            {
                SetTrackedQuest(quest);
                return;
            }
        }

        foreach (Quest quest in questMap.Values)
        {
            if (quest.IsReceived && quest.State == QuestState.CAN_FINISH)
            {
                SetTrackedQuest(quest);
                return;
            }
        }

        ClearTrackedQuest();
    }

    public void ClearTrackedQuest()
    {
        TrackedQuest = null;

        Bus<TrackedQuestChangedEvent>.Raise(new TrackedQuestChangedEvent(null));
    }

    private void RecieveQuest(QuestReceivedEvent evt)
    {
        Quest quest = GetQuestById(evt.Id);

        if (quest == null)
            return;

        quest.ReceiveQuest();

        Bus<QuestStateChangedEvent>.Raise(new QuestStateChangedEvent(quest));

        if (TrackedQuest == null)
        {
            SetTrackedQuest(quest);
        }

        Debug.Log($"Received quest: {quest.Info.ID}");
    }

    public void LoadData(GameData data)
    {
        if (!LoadQuestData)
            return;

        if (data.quests == null)
        {
            data.quests = new SerializableDictionary<string, QuestData>();
        }

        foreach (Quest quest in questMap.Values)
        {
            string questId = quest.Info.ID;

            if (data.quests.TryGetValue(questId, out QuestData questData))
            {
                quest.LoadQuestData(questData);
            }
        }

        RefreshQuestData();
    }

    public void SaveData(ref GameData data)
    {
        if (data.quests == null)
        {
            data.quests = new SerializableDictionary<string, QuestData>();
        }

        foreach (Quest quest in questMap.Values)
        {
            string questId = quest.Info.ID;
            data.quests[questId] = quest.GetQuestData();
        }
    }

    private void HandleQuestStepStateChanged(QuestStepStateChangedEvent evt)
    {
        Quest quest = GetQuestById(evt.ID);

        if (quest == null)
            return;

        quest.StoreQuestStepState(evt.QuestStepState, evt.StepIndex);

        ChangeQuestState(evt.ID, quest.State);

        // if (TrackedQuest == quest)
        // {
        //     Bus<TrackedQuestChangedEvent>.Raise(
        //         new TrackedQuestChangedEvent(quest));
        // }
    }

    private void HandlePlayerLevelUp(PlayerLevelUpEvent evt)
    {
        currentPlayerLevel = evt.Level;
    }

    private bool CheckRequirementsMet(Quest quest)
    {
        if (currentPlayerLevel < quest.Info.LevelRequirement)
            return false;

        foreach (QuestInfoSO prerequisiteQuestInfo in quest.Info.QuestPrerequisites)
        {
            Quest prerequisiteQuest = GetQuestById(prerequisiteQuestInfo.ID);

            if (prerequisiteQuest == null || prerequisiteQuest.State != QuestState.FINISHED)
                return false;
        }

        return true;
    }

    private void ChangeQuestState(string id, QuestState state)
    {
        Quest quest = GetQuestById(id);

        if (quest == null)
            return;

        quest.State = state;

        Bus<QuestStateChangedEvent>.Raise(new QuestStateChangedEvent(quest));

        // if (TrackedQuest == quest)
        // {
        //     Bus<TrackedQuestChangedEvent>.Raise(
        //         new TrackedQuestChangedEvent(quest));
        // }
    }

    private void StartQuest(StartQuestEvent evt)
    {
        Quest quest = GetQuestById(evt.Id);

        if (quest == null)
            return;

        quest.InstantiateCurrentQuestStep(transform);

        ChangeQuestState(quest.Info.ID, QuestState.IN_PROGRESS);

        if (TrackedQuest == null)
        {
            SetTrackedQuest(quest);
        }
    }

    private void AdvanceQuest(AdvanceQuestEvent evt)
    {
        Quest quest = GetQuestById(evt.Id);

        if (quest == null)
            return;

        quest.MoveToNextStep();

        

        if (quest.CurrentStepExists())
        {
            quest.InstantiateCurrentQuestStep(transform);
        }
        else
        {
            if (quest.Info.RequiresTurnIn)
            {
                ChangeQuestState(quest.Info.ID, QuestState.CAN_FINISH);
            }
            else
            {
                FinishQuest(new FinishQuestEvent(quest.Info.ID));
            }
        }

        Bus<QuestStepAdvancedEvent>.Raise(new QuestStepAdvancedEvent(quest));

        // if (TrackedQuest == quest)
        // {
        //     Bus<TrackedQuestChangedEvent>.Raise(
        //         new TrackedQuestChangedEvent(quest));
        // }
    }

    private void FinishQuest(FinishQuestEvent evt)
    {
        Quest quest = GetQuestById(evt.Id);

        if (quest == null)
            return;

        ClaimRewards(quest);

        ChangeQuestState(quest.Info.ID, QuestState.FINISHED);

        if (TrackedQuest == quest)
        {
            ClearTrackedQuest();
            AutoTrackFirstReceivedQuest();
        }
    }

    private void ClaimRewards(Quest quest)
    {
        Bus<CurrencyUpdatedEvent>.Raise(
            new CurrencyUpdatedEvent(quest.Info.GoldReward));

        PlayerStats playerStats = (PlayerStats)PlayerManager.Instance.Player.Stats;
        playerStats.AddExperience(quest.Info.ExperienceReward);

        if (quest.Info.ItemRewards == null)
            return;

        foreach (ItemReward reward in quest.Info.ItemRewards)
        {
            for (int i = 0; i < reward.Amount; i++)
            {
                Inventory.Instance.AddItem(reward.Data);
            }
        }
    }

    private Dictionary<string, Quest> CreateQuestMap()
    {
        QuestInfoSO[] allQuests = Resources.LoadAll<QuestInfoSO>("Quests");

        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();

        foreach (QuestInfoSO questInfo in allQuests)
        {
            if (idToQuestMap.ContainsKey(questInfo.ID))
            {
                Debug.LogWarning("Duplicate ID found when creating quest map: " + questInfo.ID);
                continue;
            }

            idToQuestMap.Add(questInfo.ID, new Quest(questInfo));
        }

        return idToQuestMap;
    }

    public Quest GetQuestById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (questMap.TryGetValue(id, out Quest quest))
            return quest;

        foreach (KeyValuePair<string, Quest> pair in questMap)
        {
            if (string.Equals(pair.Key, id, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        Debug.Log($"ID not found in Quest Map: {id}");
        return null;
    }

    public IEnumerable<Quest> GetReceivedQuests()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.IsReceived)
            {
                yield return quest;
            }
        }
    }
}