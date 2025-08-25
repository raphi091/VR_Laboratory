using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameData
{
    public bool IsTutorialComplete { get => C_SceneManager.I.IsTutorialCompleted; }
    public bool IsCulturingOvernight { get => GameStateManager_L.Instance.IsCulturingOvernight; }
    public ExperimentData_G currentExperimentData;
    public GameData()
    {

    }
}

public class C_DataManager : MonoBehaviour
{
    public static C_DataManager instance;

    [Header("GameData")]
    public GameData gameData;
    
    private string saveFilePath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        saveFilePath = Path.Combine(Application.persistentDataPath, "MyLabData.json");
    }

    public bool LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonData = File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(jsonData);
            if (gameData == null)
            {
                gameData = new GameData();
                return false;
            }
            return true;
        }
        return false;
    }

    public void SaveGameData()
    {
        if (gameData == null) return;
        string jsonData = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(saveFilePath, jsonData);
    }
}
