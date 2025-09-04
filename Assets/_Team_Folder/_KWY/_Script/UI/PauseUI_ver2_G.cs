using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class PauseUI_ver2_G : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject basePanel;
    public GameObject baseBtn;

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
        input = InputSystem_G.instence.input;
        mainCameraTransform = Camera.main.transform;
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

        basePanel.SetActive(true);
        input.XRILeftHand.Disable();
        input.XRILeftHandInteraction.Disable();
        input.XRILeftHandLocomotion.Disable();
        input.XRIUI.Enable();
        isPasued = true;

        SetSelectedUIElement(baseBtn);
    }

    public void CloseAllMenus()
    {
        basePanel.SetActive(false);
        input.XRILeftHand.Disable();
        input.XRILeftHandInteraction.Disable();
        input.XRILeftHandLocomotion.Disable();
        input.XRIUI.Disable();

        isPasued = false;
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
