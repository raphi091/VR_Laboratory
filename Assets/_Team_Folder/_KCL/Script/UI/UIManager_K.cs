using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;  

#endif

public class UIManager : MonoBehaviour
{
    
    public Button btnResumeGame;
    public Button btnQuitGame;

    void Start()
    {
        btnResumeGame.onClick.AddListener(OnResumeGameClicked);
        btnQuitGame.onClick.AddListener(OnQuitGameClicked);
    }

    // 게임 재개 버튼 클릭 시
    public void OnResumeGameClicked()
    {
        Time.timeScale = 1f;  
        gameObject.SetActive(false);  
    }

    // 게임 종료 버튼 클릭 시
    public void OnQuitGameClicked()
    {
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;  
#else
        Application.Quit();  
        #endif
    }
}
