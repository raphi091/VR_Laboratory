using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class PauseUI_ver2_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject settingPanel;
    public GameObject baseBtn;
    public GameObject settingSilder;

    [Header("Input Settings")]
    public InputActionReference menuAction;
    public InputActionAsset actions;

    [Header("VR Settings")]
    public float spawnDistance = 1.5f;

    [Header("Sound")]
    public AudioClip clickbtn;

    private XRInput input;
    private Transform mainCameraTransform;
    private bool isPasued = false;


    private void Awake()
    {
        input = new XRInput();
        mainCameraTransform = Camera.main.transform;
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

    private void Start()
    {
        basePanel.SetActive(false);
        settingPanel.SetActive(false);
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;

        if (isPasued)
            CloseAllMenus();
        else
            OpenMenu();
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

        // Time.timeScale = 0f;
        basePanel.SetActive(true);
        settingPanel.SetActive(false);
        input.XRIUI.Enable();
        isPasued = true;

        SetSelectedUIElement(baseBtn);
    }

    private void CloseAllMenus()
    {
        Time.timeScale = 1f;
        basePanel.SetActive(false);
        settingPanel.SetActive(false);
        input.XRIUI.Disable();

        isPasued = false;
    }

    public void OnClick_OpenSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        settingPanel.SetActive(true);

        SetSelectedUIElement(settingSilder);
    }

    public void OnClick_CloseSettings()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        settingPanel.SetActive(false);

        SetSelectedUIElement(baseBtn);
    }

    public void OnClick_ExitGame()
    {
        SoundManager_K.Instance.PlaySFX(clickbtn);
        // Time.timeScale = 1f;

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
