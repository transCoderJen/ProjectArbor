using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SaveManager : Singleton<SaveManager>
{
    [SerializeField] private string fileName;
    [SerializeField] private bool encryptData;
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
        gameData = dataHandler.Load();

        if (this.gameData == null)
        {
            Debug.Log("No save data found!");
            NewGame();
        }

        foreach(ISaveManager saveManager in saveManagers)
        {
            saveManager.LoadData(gameData);
        }
    }

    public void SaveGame()
    {
        foreach(ISaveManager saveManager in saveManagers)
        {
            saveManager.SaveData(ref gameData);
        }

        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        // SaveGame();
        // TODO Prompt the user to save before quitting
    }

    private List<ISaveManager> FindAllSaveManagers()
    {
        // This finds all MonoBehaviours in the scene, including inactive ones
        IEnumerable<ISaveManager> saveManagers = Resources.FindObjectsOfTypeAll<MonoBehaviour>().OfType<ISaveManager>();

        // Convert the IEnumerable to a List and return it
        return new List<ISaveManager>(saveManagers);
    }


    public bool HasSavedData()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        if (dataHandler.Load() != null)
        {
            return true;
        }
        
        return false;
    }
}
