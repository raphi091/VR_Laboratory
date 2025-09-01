using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class PauseUI_ExitPanel_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject baseBtn;

    [Header("Sound")]
    public AudioClip clickbtn;


    public void OnClick_CloseExitConfirm()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        gameObject.SetActive(false);
        basePanel.SetActive(true);

        SetSelectedUIElement(baseBtn);
    }

    public void OnClick_ExitGame()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}
