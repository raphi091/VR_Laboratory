using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PauseUI_G : MonoBehaviour
{
    [Header("Input Action Asset")]
    public InputActionAsset actions;

    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject settingPanel;
    public GameObject exitPanel;

    [Header("Default Selected Buttons")]
    public GameObject resumeButton;
    public GameObject settingDefaultButton;
    public GameObject exitDefaultButton;

    [Header("Input Settings")]
    public InputActionReference menuAction;

    [Header("VR Settings")]
    public Transform mainCameraTransform;
    public float spawnDistance = 1.5f;

    [Header("Sound")]
    public AudioClip clickbtn;

    private bool isPaused = false;


    private void Start()
    {
        if (basePanel != null) 
            basePanel.SetActive(false);

        if (settingPanel != null) 
            settingPanel.SetActive(false);

        if (exitPanel != null) 
            exitPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed += OnMenuButtonPressed;
            menuAction.action.Enable();
        }

        actions.FindActionMap("XRI LeftHand Locomotion").Enable();
        actions.FindActionMap("XRI UI").Disable();
    }

    private void OnDisable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed -= OnMenuButtonPressed;
            menuAction.action.Disable();
        }

        actions.FindActionMap("XRI LeftHand Locomotion").Disable();
        actions.FindActionMap("XRI UI").Disable();
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;

        if (isPaused)
        {
            CloseAllMenus();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        if (mainCameraTransform != null)
        {
            Vector3 forward = mainCameraTransform.forward;
            forward.y = 0;
            transform.position = mainCameraTransform.position + forward.normalized * spawnDistance;
            float cameraYRotation = mainCameraTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, cameraYRotation, 0f);
        }

        basePanel.SetActive(true);
        settingPanel.SetActive(false);
        exitPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
        actions.FindActionMap("XRI LeftHand Locomotion").Disable();
        actions.FindActionMap("XRI UI").Enable();

        SetSelectedUIElement(resumeButton);
    }

    public void CloseAllMenus()
    {
        basePanel.SetActive(false);
        settingPanel.SetActive(false);
        exitPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        actions.FindActionMap("XRI UI").Disable();
        actions.FindActionMap("XRI LeftHand Locomotion").Enable();
    }

    public void OnClick_OpenSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        basePanel.SetActive(false);
        settingPanel.SetActive(true);

        SetSelectedUIElement(settingDefaultButton);
    }

    public void OnClick_CloseSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        settingPanel.SetActive(false);
        basePanel.SetActive(true);
    }

    public void OnClick_OpenExitConfirm()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        basePanel.SetActive(false);
        exitPanel.SetActive(true);

        SetSelectedUIElement(exitDefaultButton);
    }

    public void OnClick_CloseExitConfirm()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        exitPanel.SetActive(false);
        basePanel.SetActive(true);
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
        if (element != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(element);
        }
    }
}