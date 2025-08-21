using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMByScene : MonoBehaviour
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
        if (SoundManager.Instance == null) return;

        string name = scene.name;

        // "Lobby" 포함 → Lobby 트랙
        if (name.Contains("Lobby"))
        {
            SoundManager.Instance.PlayBGM(BGMTrackName.Lobby, true);
        }
        // 그 외 전부 → Tutorial 트랙(기본)
        else
        {
            SoundManager.Instance.PlayBGM(BGMTrackName.Tutorial, true);
        }
    }
}
