using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class PauseUI_BasePanel_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingPanel;
    public GameObject exitPanel;
    public GameObject settingSilder;
    public GameObject exitBtn;

    [Header("Input Settings")]
    public InputActionReference menuAction;
    public InputActionAsset actions;

    [Header("Sound")]
    public AudioClip clickbtn;

    private XRInput input;


    private void Awake()
    {
        input = new XRInput();
    }

    private void OnEnable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed += OnMenuButtonPressed;
            menuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed -= OnMenuButtonPressed;
            menuAction.action.Disable();
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;

        CloseAllMenus();
    }

    public void OnClick_Resume()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        CloseAllMenus();
    }

    public void OnClick_OpenSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        gameObject.SetActive(false);
        settingPanel.SetActive(true);

        SetSelectedUIElement(settingSilder);
    }

    public void OnClick_OpenExitConfirm()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        gameObject.SetActive(false);
        exitPanel.SetActive(true);

        SetSelectedUIElement(exitBtn);
    }

    private void CloseAllMenus()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        settingPanel.SetActive(false);
        exitPanel.SetActive(false);
        input.XRIUI.Disable();
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}
