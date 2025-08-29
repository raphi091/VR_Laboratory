using UnityEngine;

public class GameStateManager_L : MonoBehaviour
{
    public static GameStateManager_L Instance { get; private set; }

    public bool IsCulturingOvernight { get; set; }
    
    public string IncubatedPetriDishID { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}