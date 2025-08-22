using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class TorchController_G : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("토치 끝에 붙어있는 불꽃 VFX")]
    public VisualEffect flameVFX;

    [Tooltip("상호작용에 사용할 컨트롤러 버튼")]
    public InputActionReference interactionAction;

    [Header("상태")]
    [Tooltip("현재 토치가 켜져 있는지 여부")]
    public bool isLit = false;

    private bool isHeld = false;

    private void OnEnable()
    {
        interactionAction.action.started += LightTorch;
        interactionAction.action.canceled += ExtinguishTorch;
    }

    private void OnDisable()
    {
        interactionAction.action.started -= LightTorch;
        interactionAction.action.canceled -= ExtinguishTorch;
    }

    private void Start()
    {
        if (flameVFX != null)
        {
            flameVFX.Stop();
            flameVFX.gameObject.SetActive(false);
        }

        isLit = false;
    }

    private void LightTorch(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        if (flameVFX != null)
        {
            flameVFX.gameObject.SetActive(true);
            flameVFX.SendEvent("OnPlay");
        }

        isLit = true;
    }

    private void ExtinguishTorch(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        if (flameVFX != null)
        {
            flameVFX.SendEvent("OnStop");
        }

        isLit = false;
    }

    public void OnGrab()
    {
        isHeld = true;
    }

    public void OnRelease()
    {
        isHeld = false;
    }
}
