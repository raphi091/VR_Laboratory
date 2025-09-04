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
        IsTutorialCompleted = false;
        IsCulturingOvernight = false;
    }
}

public class C_DataManager : Ch_BehaviourSingleton<C_DataManager>
{
    protected override bool IsDontdestroy()
    {
        return true;
    }

    [Header("GameData")]
    public GameData gameData=new GameData();
    
    private string saveFilePath;

    protected override void Awake()
    {
        base.Awake();
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
