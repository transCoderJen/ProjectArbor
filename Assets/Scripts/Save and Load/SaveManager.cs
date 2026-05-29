using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ShiftedSignal.Garden.Misc;

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

        [HideInInspector]
        public GameData gameData;

        private List<ISaveManager> saveManagers;
        private FileDataHandler dataHandler;

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

            foreach (ISaveManager saveManager in saveManagers)
            {
                saveManager.LoadData(gameData);
            }
        }

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            foreach (ISaveManager saveManager in saveManagers)
            {
                saveManager.SaveData(ref gameData);
            }

            dataHandler.Save(gameData);
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