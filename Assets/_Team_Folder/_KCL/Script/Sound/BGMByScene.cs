using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMByScene : MonoBehaviour
{
    // 이 오브젝트는 씬마다 하나 두거나, 최초 씬에서 DontDestroyOnLoad로 유지해도 됨.
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
        if (name.Contains("Lobby"))
            SoundManager.Instance.PlayBGM(BGMTrackName.Lobby, true);
        else if (name.Contains("Lab") || name.Contains("Experiment"))
            SoundManager.Instance.PlayBGM(BGMTrackName.LabIdle, true);
        else if (name.Contains("Ending"))
            SoundManager.Instance.PlayBGM(BGMTrackName.Ending, false);
        else
            SoundManager.Instance.PlayBGM(BGMTrackName.LabIdle, true); // 기본값
    }
}
