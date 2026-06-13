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

        [Header("Teleport Locations")]
        [SerializeField] private Transform StartTeleportPoint;
        [SerializeField] private Transform FarmTeleportPoint;

        private readonly List<string> outputHistory = new();
        private readonly Queue<string> pendingOutputLines = new();
        private readonly List<string> commandHistory = new();

        private int commandHistoryIndex = -1;

        private CharacterHealth playerHealth;
        private Player player;

        private bool isOpen;
        private bool noclipEnabled;
        private bool waitingForNextPage;

        private void Start()
        {
            if (consoleRoot != null)
                consoleRoot.SetActive(false);

            CachePlayer();
            ClearOutput();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
                ToggleConsole();

            if (!isOpen)
                return;

            if (waitingForNextPage)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                    ShowNextOutputPage();

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
                ExecuteCommand(inputField.text);
        }

        private void CachePlayer()
        {
            player = Player.Instance;

            if (player == null)
                return;

            playerHealth = player.GetComponent<CharacterHealth>();

            if (playerCollider == null)
                playerCollider = player.GetComponent<Collider>();

            if (playerRigidbody == null)
                playerRigidbody = player.GetComponent<Rigidbody>();
        }

        private void ToggleConsole()
        {
            isOpen = !isOpen;

            if (consoleRoot != null)
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
                commandHistory.Add(command);

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

                case "player.health":
                    PrintPlayerHealth();
                    return;

                case "player.heal.full":
                    FullHeal();
                    return;

                case "player.kill":
                    KillPlayer();
                    return;
            }

            if (lower.StartsWith("player.damage"))
            {
                HandlePlayerDamageCommand(command);
                return;
            }

            if (lower.StartsWith("player.heal "))
            {
                HandlePlayerHealCommand(command);
                return;
            }

            if (lower.StartsWith("player.hearts.max"))
            {
                HandleMaxHeartsCommand(command);
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

            if (lower.StartsWith("tp"))
            {
                HandleTeleportCommand(command);
                return;
            }

            Log("Unknown command. Type 'help'.");
        }

        private void ShowHelp()
        {
            Log("=== PLAYER ===");
            Log("player.health");
            Log("player.damage 1");
            Log("player.heal 1");
            Log("player.heal.full");
            Log("player.hearts.max 5");
            Log("player.kill");

            Log("=== ITEMS / CURRENCY ===");
            Log("currency.add 1000");
            Log("item.add Wood 50");
            Log("item.add \"Blood Essence\" 10");

            Log("=== NOCLIP ===");
            Log("noclip on");
            Log("noclip off");
            Log("noclip toggle");

            Log("=== QUESTS ===");
            Log("quest.receive QuestID");
            Log("quest.start QuestID");
            Log("quest.advance QuestID");
            Log("quest.finish QuestID");
            Log("quest.status QuestID");

            Log("=== TELEPORT ===");
            Log("tp start");
            Log("tp farm");
        }

        private void PrintPlayerHealth()
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            Log("=== PLAYER HEALTH ===");
            Log($"Hearts: {playerHealth.CurrentHearts}/{playerHealth.MaxHearts}");

            if (player != null)
                Log($"Attack Damage: {player.AttackDamage}");
        }

        private void HandlePlayerDamageCommand(string command)
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !int.TryParse(parts[1], out int damage))
            {
                Log("Usage: player.damage Amount");
                return;
            }

            playerHealth.TakeDamage(damage, true);
            Log($"Player took {damage} damage.");
        }

        private void HandlePlayerHealCommand(string command)
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !int.TryParse(parts[1], out int amount))
            {
                Log("Usage: player.heal Amount");
                return;
            }

            playerHealth.Heal(amount);
            Log($"Player healed {amount} hearts.");
        }

        private void FullHeal()
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            playerHealth.RestoreFullHealth();
            Log("Player healed to full.");
        }

        private void HandleMaxHeartsCommand(string command)
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !int.TryParse(parts[1], out int maxHearts))
            {
                Log("Usage: player.hearts.max Amount");
                return;
            }

            playerHealth.SetMaxHearts(maxHearts, true);
            Log($"Player max hearts set to {maxHearts}.");
        }

        private void KillPlayer()
        {
            CachePlayer();

            if (playerHealth == null)
            {
                Log("Player health not found.");
                return;
            }

            playerHealth.TakeDamage(9999, false);
            Log("Player killed.");
        }

        private void HandleCurrencyCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !int.TryParse(parts[1], out int amount))
            {
                Log("Usage: currency.add Amount");
                return;
            }

            Bus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(amount));
            Log($"Added {amount} gold.");
        }

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

            ItemData item = FindItemByNameOrID(itemName);

            if (item == null)
            {
                Log($"Item not found: {itemName}");
                return;
            }

            Inventory.Instance.AddItem(item, amount, true);
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

        private void HandleNoclipCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                Log("Usage: noclip on/off/toggle");
                return;
            }

            switch (parts[1].ToLower())
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

        private void HandleQuestCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

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
                    Bus<QuestReceivedEvent>.Raise(new QuestReceivedEvent(questID));
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
                    break;
            }
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

        private void LogQuestStatus(Quest quest)
        {
            Log("=== QUEST STATUS ===");
            Log($"ID: {quest.Info.ID}");
            Log($"Name: {quest.Info.DisplayName}");
            Log($"State: {quest.State}");
            Log($"Received: {quest.IsReceived}");
        }

        private void HandleTeleportCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                Log("Usage: tp start");
                Log("Usage: tp farm");
                return;
            }

            CachePlayer();

            if (player == null)
            {
                Log("Player not found.");
                return;
            }

            string destinationName = parts[1].ToLower();

            Transform destination = destinationName switch
            {
                "start" => StartTeleportPoint,
                "farm" => FarmTeleportPoint,
                _ => null
            };

            if (destination == null)
            {
                Log($"Unknown destination: {destinationName}");
                return;
            }

            player.transform.position = destination.position;

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            Log($"Teleported to {destinationName}");
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

            while (pendingOutputLines.Count > 0 && visibleLines.Count < maxOutputLines)
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