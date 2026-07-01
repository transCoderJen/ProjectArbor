using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.SaveAndLoad
{
    public class SaveManager : Singleton<SaveManager>
    {
        [Header("Save Settings")]
        [SerializeField] private string fileName;
        [SerializeField] private bool encryptData;

        [Header("Debug")]
        [SerializeField] private bool LoadAsNewGame;

        [HideInInspector] public GameData gameData;

        private GameData runtimeGameData;
        private List<ISaveManager> saveManagers;
        private FileDataHandler dataHandler;
        private HashSet<string> triggeredDialogueIds = new();

        [ContextMenu("Delete save file")]
        public void DeleteSavedData()
        {
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
            dataHandler.Delete();
        }

        private void Start()
        {
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
            saveManagers = FindAllSaveManagers();
            LoadGame();
        }

        public void NewGame()
        {
            gameData = new GameData();
            triggeredDialogueIds.Clear();
        }

        public void LoadGame()
        {
            if (LoadAsNewGame)
            {
                Debug.Log("Load As New Game enabled. Creating fresh save data.");
                NewGame();
            }
            else
            {
                gameData = dataHandler.Load();

                if (gameData == null)
                {
                    Debug.Log("No save data found!");
                    NewGame();
                }
            }

            LoadTriggeredDialogueIds();

            foreach (ISaveManager saveManager in saveManagers)
                saveManager.LoadData(gameData);
        }

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            saveManagers = FindAllSaveManagers();

            foreach (ISaveManager saveManager in saveManagers)
                saveManager.SaveData(ref gameData);

            SaveTriggeredDialogueIds();

            dataHandler.Save(gameData);
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        public void CaptureWorldRuntimeData()
        {
            if (gameData == null)
                gameData = new GameData();

            runtimeGameData = gameData;
            saveManagers = FindAllSaveManagers();

            foreach (ISaveManager saveManager in saveManagers)
            {
                if (ShouldSkipForWorldRuntime(saveManager))
                    continue;

                saveManager.SaveData(ref runtimeGameData);
            }

            SaveTriggeredDialogueIds();
        }

        public void LoadWorldRuntimeData()
        {
            if (runtimeGameData == null)
                return;

            gameData = runtimeGameData;
            saveManagers = FindAllSaveManagers();

            foreach (ISaveManager saveManager in saveManagers)
            {
                if (ShouldSkipForWorldRuntime(saveManager))
                    continue;

                saveManager.LoadData(gameData);
            }

            LoadTriggeredDialogueIds();
        }

        private bool ShouldSkipForWorldRuntime(ISaveManager saveManager)
        {
            return saveManager is Player ||
                   saveManager is PlayerManager;
        }

        public bool HasDialogueTriggerBeenUsed(string triggerId)
        {
            if (string.IsNullOrWhiteSpace(triggerId))
                return false;

            return triggeredDialogueIds.Contains(NormalizeDialogueTriggerId(triggerId));
        }

        public void MarkDialogueTriggerUsed(string triggerId)
        {
            if (string.IsNullOrWhiteSpace(triggerId))
                return;

            triggeredDialogueIds.Add(NormalizeDialogueTriggerId(triggerId));
            SaveTriggeredDialogueIds();
        }

        private void LoadTriggeredDialogueIds()
        {
            triggeredDialogueIds.Clear();

            if (gameData.TriggeredDialogueIds == null)
            {
                gameData.TriggeredDialogueIds = new List<string>();
                return;
            }

            foreach (string id in gameData.TriggeredDialogueIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    triggeredDialogueIds.Add(NormalizeDialogueTriggerId(id));
            }
        }

        private void SaveTriggeredDialogueIds()
        {
            gameData.TriggeredDialogueIds = triggeredDialogueIds.ToList();
        }

        private string NormalizeDialogueTriggerId(string triggerId)
        {
            return triggerId.Trim().ToLower();
        }

        [ContextMenu("Open Save File Location")]
        private void OpenSaveFileLocation()
        {
            string path = Application.persistentDataPath;

#if UNITY_EDITOR
            EditorUtility.RevealInFinder(path);
#else
            Application.OpenURL("file://" + path);
#endif
        }

        private void OnApplicationQuit()
        {
            // SaveGame();
            // TODO Prompt the user to save before quitting
        }

        private List<ISaveManager> FindAllSaveManagers()
        {
            IEnumerable<ISaveManager> saveManagers =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                ).OfType<ISaveManager>();

            return new List<ISaveManager>(saveManagers);
        }

        public bool HasSavedData()
        {
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
            return dataHandler.Load() != null;
        }
    }
}