using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.QuestSystem;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.UserInterface;

namespace ShiftedSignal.Garden.Debugging
{
    public class DebugConsole : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject consoleRoot;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text outputText;

        [Header("Output")]
        [SerializeField] private int maxOutputLines = 10;

        [Header("Noclip")]
        [SerializeField] private Collider playerCollider;
        [SerializeField] private Rigidbody playerRigidbody;

        private readonly List<string> outputHistory = new();
        private readonly Queue<string> pendingOutputLines = new();

        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;

        private CharacterStats playerStats;
        private bool isOpen;
        private bool noclipEnabled;
        private bool waitingForNextPage;

        private void Start()
        {
            consoleRoot.SetActive(false);

            if (Player.Instance != null)
            {
                playerStats = Player.Instance.GetComponent<CharacterStats>();

                if (playerCollider == null)
                    playerCollider = Player.Instance.GetComponent<Collider>();

                if (playerRigidbody == null)
                    playerRigidbody = Player.Instance.GetComponent<Rigidbody>();
            }

            ClearOutput();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                ToggleConsole();
            }

            if (!isOpen)
                return;

            if (waitingForNextPage)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ShowNextOutputPage();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                CycleCommandHistory(-1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                CycleCommandHistory(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteCommand(inputField.text);
            }
        }

        private void CycleCommandHistory(int direction)
        {
            if (commandHistory.Count == 0)
                return;

            if (commandHistoryIndex == -1)
                commandHistoryIndex = commandHistory.Count;

            commandHistoryIndex += direction;

            if (commandHistoryIndex < 0)
                commandHistoryIndex = commandHistory.Count - 1;

            if (commandHistoryIndex >= commandHistory.Count)
            {
                commandHistoryIndex = commandHistory.Count;
                inputField.text = "";
                inputField.ActivateInputField();
                inputField.caretPosition = inputField.text.Length;
                return;
            }

            inputField.text = commandHistory[commandHistoryIndex];
            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;
        }

        private void ToggleConsole()
        {
            isOpen = !isOpen;
            consoleRoot.SetActive(isOpen);

            if (isOpen)
            {
                Bus<EnablePlayerMovementEvent>.Raise(new EnablePlayerMovementEvent(false));
                Time.timeScale = 0f;

                if (UI.Instance != null)
                    UI.Instance.DeactivateAllMenus();

                inputField.text = "";
                inputField.ActivateInputField();
                inputField.Select();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Bus<EnablePlayerMovementEvent>.Raise(new EnablePlayerMovementEvent(true));
                Time.timeScale = 1f;

                if (UI.Instance != null)
                    UI.Instance.SwitchToInGameUI();
            }
        }

        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            command = command.Trim();

            if (commandHistory.Count == 0 || commandHistory[^1] != command)
            {
                commandHistory.Add(command);
            }

            commandHistoryIndex = commandHistory.Count;

            BeginCommandOutput();

            Log("> " + command);

            try
            {
                ParseCommand(command);
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
            }

            EndCommandOutput();

            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }

        private string NormalizeQuestID(string questID)
        {
            if (string.IsNullOrWhiteSpace(questID))
                return questID;

            questID = questID.Trim();

            if (!questID.EndsWith("quest", StringComparison.OrdinalIgnoreCase))
                questID += "quest";

            return questID;
        }

        private void ParseCommand(string command)
        {
            string lower = command.ToLower();

            switch (lower)
            {
                case "help":
                    ShowHelp();
                    return;

                case "clear":
                    ClearOutput();
                    return;

                case "player.stats":
                    PrintStats();
                    return;

                case "player.heal.full":
                    FullHeal();
                    return;

                case "player.mp.full":
                    FullMana();
                    return;

                case "player.kill":
                    KillPlayer();
                    return;
            }

            if (lower.StartsWith("player.stat"))
            {
                HandlePlayerStatCommand(command);
                return;
            }

            if (lower.StartsWith("currency.add"))
            {
                HandleCurrencyCommand(command);
                return;
            }

            if (lower.StartsWith("item.add"))
            {
                HandleAddItemCommand(command);
                return;
            }

            if (lower.StartsWith("noclip"))
            {
                HandleNoclipCommand(command);
                return;
            }

            if (lower.StartsWith("quest."))
            {
                HandleQuestCommand(command);
                return;
            }

            Log("Unknown command. Type 'help'.");
        }

        #region Help

        private void ShowHelp()
        {
            Log("=== COMMANDS ===");

            Log("help");
            Log("clear");

            Log("player.stat Speed 20");
            Log("player.stat Power 50");
            Log("player.stat Defense 999");
            Log("player.stat MaxHP 500");
            Log("player.stat MaxMP 200");
            Log("player.stat CritChance 100");
            Log("player.stat CritPower 300");
            Log("player.heal.full");
            Log("player.mp.full");
            Log("player.kill");

            Log("currency.add 1000");

            Log("item.add Wood 50");
            Log("item.add Stone 25");
            Log("item.add \"Blood Essence\" 10");

            Log("noclip on");
            Log("noclip off");
            Log("noclip toggle");

            Log("quest.receive QuestID");
            Log("quest.start QuestID");
            Log("quest.advance QuestID");
            Log("quest.finish QuestID");
            Log("quest.status QuestID");
        }

        #endregion

        #region Currency

        private void HandleCurrencyCommand(string command)
        {
            string[] parts = command.Split(' ');

            if (parts.Length < 2)
            {
                Log("Usage: currency.add Amount");
                return;
            }

            if (!int.TryParse(parts[1], out int amount))
            {
                Log("Invalid amount.");
                return;
            }

            Bus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(amount));

            Log($"Added {amount} gold.");
        }

        #endregion

        #region Player Stats

        private void PrintStats()
        {
            if (playerStats == null)
            {
                Log("Player stats not found.");
                return;
            }

            Log("=== PLAYER STATS ===");

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                Stat stat = playerStats.GetStat(statType);
                Log($"{statType}: {stat.GetValue()}");
            }

            Log($"Health: {playerStats.CurrentHealth}/{playerStats.GetMaxHealthValue()}");
            Log($"MP: {playerStats.CurrentMP}/{playerStats.MaxMP.GetValue()}");
        }

        private void FullHeal()
        {
            if (playerStats == null)
            {
                Log("Player stats not found.");
                return;
            }

            playerStats.IncreaseHealthBy(playerStats.GetMaxHealthValue());
            Log("Player healed to full.");
        }

        private void FullMana()
        {
            if (playerStats == null)
            {
                Log("Player stats not found.");
                return;
            }

            playerStats.IncreaseMagicBy(playerStats.MaxMP.GetValue());
            Log("Player MP restored.");
        }

        private void KillPlayer()
        {
            if (playerStats == null)
            {
                Log("Player stats not found.");
                return;
            }

            playerStats.DecreaseHealthBy(999999);
            Log("Player killed.");
        }

        private void HandlePlayerStatCommand(string command)
        {
            // player.stat Speed 20

            if (playerStats == null)
            {
                Log("Player stats not found.");
                return;
            }

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                Log("Usage: player.stat Speed 20");
                return;
            }

            string statName = parts[1];
            string valueText = parts[2];

            if (!Enum.TryParse(statName, true, out StatType statType))
            {
                Log($"Invalid stat: {statName}");
                return;
            }

            if (!int.TryParse(valueText, out int newValue))
            {
                Log("Invalid stat value.");
                return;
            }

            Stat stat = playerStats.GetStat(statType);

            int oldValue = stat.GetValue();
            int difference = newValue - oldValue;

            stat.AddModifier(difference);

            if (statType == StatType.MaxHP)
            {
                playerStats.CurrentHealth = playerStats.GetMaxHealthValue();
                playerStats.OnHealthChanged?.Invoke();
            }

            if (statType == StatType.MaxMP)
            {
                playerStats.CurrentMP = playerStats.MaxMP.GetValue();
                playerStats.OnMagicChanged?.Invoke();
            }

            Log($"{statType} changed from {oldValue} to {newValue}");
        }

        #endregion

        #region Inventory

        private void HandleAddItemCommand(string command)
        {
            string withoutPrefix = command.Replace("item.add", "").Trim();

            if (string.IsNullOrWhiteSpace(withoutPrefix))
            {
                Log("Usage: item.add ItemName Amount");
                return;
            }

            string itemName;
            int amount = 1;

            string[] parts = withoutPrefix.Split(' ');

            if (parts.Length > 1 && int.TryParse(parts[^1], out int parsedAmount))
            {
                amount = parsedAmount;
                itemName = string.Join(" ", parts, 0, parts.Length - 1);
            }
            else
            {
                itemName = withoutPrefix;
            }

            itemName = itemName.Replace("\"", "").Trim();

            if (amount <= 0)
            {
                Log("Amount must be greater than 0.");
                return;
            }

            ItemData item = FindItemByNameOrID(itemName);

            if (item == null)
            {
                Log($"Item not found: {itemName}");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                Inventory.Instance.AddItem(item, false);
            }

            Inventory.Instance.AddItem(item, true);
            Inventory.Instance.RemoveItem(item, false);

            Log($"Added {amount} x {item.name}");
        }

        private ItemData FindItemByNameOrID(string itemNameOrID)
        {
            if (Inventory.Instance == null)
                return null;

            foreach (ItemData item in Inventory.Instance.itemDataBase)
            {
                if (item == null)
                    continue;

                if (string.Equals(item.name, itemNameOrID, StringComparison.OrdinalIgnoreCase))
                    return item;

                if (string.Equals(item.ItemID, itemNameOrID, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        #endregion

        #region Noclip

        private void HandleNoclipCommand(string command)
        {
            string[] parts = command.Split(' ');

            if (parts.Length < 2)
            {
                Log("Usage: noclip on/off/toggle");
                return;
            }

            string mode = parts[1].ToLower();

            switch (mode)
            {
                case "on":
                    SetNoclip(true);
                    break;

                case "off":
                    SetNoclip(false);
                    break;

                case "toggle":
                    SetNoclip(!noclipEnabled);
                    break;

                default:
                    Log("Usage: noclip on/off/toggle");
                    break;
            }
        }

        private void SetNoclip(bool enabled)
        {
            noclipEnabled = enabled;

            if (playerCollider != null)
                playerCollider.enabled = !enabled;

            if (playerRigidbody != null)
            {
                playerRigidbody.useGravity = !enabled;
                playerRigidbody.linearVelocity = Vector3.zero;
            }

            Log(enabled ? "Noclip enabled." : "Noclip disabled.");
        }

        #endregion

        #region Quests

        private void HandleQuestCommand(string command)
        {
            string[] parts = command.Split(' ');

            if (parts.Length < 2)
            {
                Log("Usage: quest.start QuestID");
                return;
            }

            string questCommand = parts[0].ToLower();
            string questID = NormalizeQuestID(parts[1]);

            Quest quest = QuestManager.Instance.GetQuestById(questID);

            if (quest == null)
            {
                Log($"Quest not found: {questID}");
                return;
            }

            switch (questCommand)
            {
                case "quest.receive":
                case "quest.recieve":
                    Bus<QuestReceivedEvent>.Raise(new QuestReceivedEvent(questID));
                    Log($"Received quest: {questID}");
                    break;

                case "quest.start":
                    Bus<StartQuestEvent>.Raise(new StartQuestEvent(questID));
                    Log($"Started quest: {questID}");
                    break;

                case "quest.advance":
                    Bus<AdvanceQuestEvent>.Raise(new AdvanceQuestEvent(questID));
                    Log($"Advanced quest: {questID}");
                    break;

                case "quest.finish":
                    Bus<FinishQuestEvent>.Raise(new FinishQuestEvent(questID));
                    Log($"Finished quest: {questID}");
                    break;

                case "quest.status":
                    LogQuestStatus(quest);
                    break;

                default:
                    Log("Unknown quest command.");
                    Log("Use: quest.receive/start/advance/finish/status QuestID");
                    break;
            }
        }

        private void LogQuestStatus(Quest quest)
        {
            Log("=== QUEST STATUS ===");
            Log($"ID: {quest.Info.ID}");
            Log($"Name: {quest.Info.DisplayName}");
            Log($"State: {quest.State}");
            Log($"Received: {quest.IsReceived}");
        }

        #endregion

        #region Output Paging

        private void BeginCommandOutput()
        {
            pendingOutputLines.Clear();
            waitingForNextPage = false;
        }

        private void Log(string message)
        {
            pendingOutputLines.Enqueue(message);
        }

        private void EndCommandOutput()
        {
            ShowNextOutputPage();
        }

        private void ShowNextOutputPage()
        {
            List<string> visibleLines = new();

            int linesToShow = maxOutputLines;

            while (pendingOutputLines.Count > 0 && visibleLines.Count < linesToShow)
            {
                string line = pendingOutputLines.Dequeue();
                visibleLines.Add(line);
                outputHistory.Add(line);
            }

            waitingForNextPage = pendingOutputLines.Count > 0;

            if (waitingForNextPage)
            {
                visibleLines.Add("");
                visibleLines.Add("[Press Space To Continue]");
            }

            outputText.text = string.Join("\n", visibleLines);

            inputField.ActivateInputField();
            inputField.Select();
        }

        private void ClearOutput()
        {
            outputHistory.Clear();
            pendingOutputLines.Clear();
            waitingForNextPage = false;

            outputText.text = "";
        }

        #endregion

        private void OnDisable()
        {
            Time.timeScale = 1f;

            if (UI.Instance != null)
                UI.Instance.SwitchToInGameUI();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;

            if (UI.Instance != null)
                UI.Instance.SwitchToInGameUI();
        }
    }
}