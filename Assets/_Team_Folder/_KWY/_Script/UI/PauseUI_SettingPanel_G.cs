using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class PauseUI_SettingPanel_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject baseBtn;

    [Header("Sound")]
    public AudioClip clickbtn;


    public void OnClick_CloseSettings()
    {
        Debug.Log(7);
        SoundManager_K.Instance.PlaySFX(clickbtn);
        gameObject.SetActive(false);
        basePanel.SetActive(true);

        SetSelectedUIElement(baseBtn);
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}
