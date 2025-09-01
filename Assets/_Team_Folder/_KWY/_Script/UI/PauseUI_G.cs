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
    [Header("UI 연결")]
    public GameObject basePanel;
    public GameObject settingPanel;
    public GameObject exitPanel;
    public GameObject baseBtn;
    public GameObject settingSlider;
    public GameObject exitBtn;

    [Header("위치")]
    public float offset = 2f;

    [Header("Input")]
    public InputActionReference menuAction;

    [Header("버튼 클릭 사운드")]
    public AudioClip clickbtn;

    private XRInput input;
    private bool isPaused = false;
    public bool IsPaused => isPaused;


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
            menuAction?.action.Disable();
        }
    }

    private void Start()
    {
        SetUpMenu();
    }

    private void SetUpMenu()
    {
        if (basePanel != null)
            basePanel.SetActive(false);

        if (settingPanel != null)
            settingPanel.SetActive(false);

        if (exitPanel != null)
            exitPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;

        if (isPaused)
        {
            OnCloseMenu();
        }
        else
        {
            Transform cameraTransform = Camera.main.transform;

            transform.position = cameraTransform.position + (cameraTransform.forward * offset);
            Vector3 targetDirection = transform.position - cameraTransform.position;
            targetDirection.y = 0;
            transform.rotation = Quaternion.LookRotation(targetDirection);

            OnOpenMenu();
        }
    }

    public void OnOpenMenu()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        if (basePanel != null)
            basePanel.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
        input.XRIUI.Enable();

        SetSelectedUIElement(baseBtn);
    }

    public void OnCloseMenu()
    {
        if (basePanel != null)
            basePanel.SetActive(false);

        if (basePanel != null)
            basePanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        input.XRIUI.Disable();
    }

    public void OnSettingBtn()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            SetSelectedUIElement(settingSlider);
        } 
    }

    public void OnExitBtn()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
            SetSelectedUIElement(exitBtn);
        }
    }

    public void OnExit()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        SetUpMenu();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBack()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);

        if (basePanel != null)
        {
            settingPanel.SetActive(false);
            exitPanel.SetActive(false);
            basePanel.SetActive(true);
            SetSelectedUIElement(baseBtn);
        }
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}