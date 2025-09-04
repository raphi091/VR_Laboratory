using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem_G : MonoBehaviour
{
    public static InputSystem_G instence = null;

    [HideInInspector] public XRInput input;

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
}
