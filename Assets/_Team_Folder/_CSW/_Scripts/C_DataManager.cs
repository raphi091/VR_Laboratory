using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameData
{
    public bool IsTutorialCompleted { get; set; }
    public bool IsCulturingOvernight { get; set; }
    public ExperimentData_G currentExperimentData;
    public GameData()
    {

    }
}

public class C_DataManager : Ch_BehaviourSingleton<C_DataManager>
{
    protected override bool IsDontdestroy()
    {
        return true;
    }

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
