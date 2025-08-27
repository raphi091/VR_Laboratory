using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMByScene_K : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SoundManager_K.Instance == null) return;

        string name = scene.name;

        // "Lobby" 포함 → Lobby 트랙
        if (name.Contains("Lobby"))
        {
            SoundManager_K.Instance.PlayBGM(BGMTrackName.Lobby, true);
        }
        // 그 외 전부 → Tutorial 트랙(기본)
        else
        {
            SoundManager_K.Instance.PlayBGM(BGMTrackName.Tutorial, true);
        }
    }
}
