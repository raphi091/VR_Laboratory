using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem_G : MonoBehaviour
{
    public static InputSystem_G instence = null;

    [HideInInspector] public XRInput input;
    public InputActionAsset action;

    private void Awake()
    {
        input = new XRInput();

        if (instence == null)
            instence = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    public void SetLocomotionEnabled(bool enabled)
    {
        if (!action) return;

        var lh = action.FindActionMap("XRI RightHand Locomotion", throwIfNotFound: false);
        var rh = action.FindActionMap("XRI LeftHand Locomotion", throwIfNotFound: false);

        if (lh != null)
        { 
            if (enabled) 
                lh.Enable();
            else 
                lh.Disable(); 
        }

        if (rh != null) 
        { 
            if (enabled) 
                rh.Enable();
            else 
                rh.Disable();
        }
    }
}
