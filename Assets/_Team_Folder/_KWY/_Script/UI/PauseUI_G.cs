using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class PauseUI_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject settingPanel;
    public GameObject exitPanel;
    public GameObject baseBtn;

    [Header("Input Settings")]
    public InputActionReference menuAction;
    public InputActionAsset actions;

    [Header("VR Settings")]
    public Transform mainCameraTransform;
    public float spawnDistance = 1.5f;

    [Header("Sound")]
    public AudioClip clickbtn;

    public bool isPaused = false;

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

        if (!isPaused)
            OpenMenu();
    }

    private void OpenMenu()
    {
        Debug.Log(8);
        if (mainCameraTransform != null)
        {
            Vector3 forward = mainCameraTransform.forward;
            forward.y = 0;
            transform.position = mainCameraTransform.position + forward.normalized * spawnDistance;
            float cameraYRotation = mainCameraTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, cameraYRotation, 0f);
        }

        Time.timeScale = 0f;
        basePanel.SetActive(true);
        settingPanel.SetActive(false);
        exitPanel.SetActive(false);
        input.XRIUI.Enable();
        isPaused = true;

        SetSelectedUIElement(baseBtn);
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}