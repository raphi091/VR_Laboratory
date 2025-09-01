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
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject settingPanel;
    public GameObject exitPanel;

    [Header("Input Settings")]
    public InputActionReference menuAction;
    public InputActionAsset actions;

    [Header("VR Settings")]
    public Transform mainCameraTransform;
    public float spawnDistance = 1.5f;

    [Header("Sound")]
    public AudioClip clickbtn;

    private bool isPaused = false;
    public bool IsPaused => isPaused;


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

        if (isPaused) 
            CloseAllMenus();
        else 
            OpenMenu();
    }

    public void OnClick_Resume()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        CloseAllMenus();
    }

    public void OnClick_OpenSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        basePanel.SetActive(false);
        settingPanel.SetActive(true);
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

    private void OpenMenu()
    {
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

        isPaused = true;
    }

    private void CloseAllMenus()
    {
        basePanel.SetActive(false);
        settingPanel.SetActive(false);
        exitPanel.SetActive(false);

        isPaused = false;
    }
}