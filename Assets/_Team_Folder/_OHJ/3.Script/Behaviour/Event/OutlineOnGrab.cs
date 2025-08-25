using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Outline))]
public class OutlineOnGrab : XRGrabInteractable
{
    private Outline outline;
    [SerializeField] private Color outlineColor;

    protected override void Awake()
    {
        base.Awake();
        outline = GetComponent<Outline>();

        if(outline != null)
        {
            outline.enabled = false;
            outline.OutlineColor = outlineColor;
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        Debug.LogWarning($"Hover");
        if(outline != null)
        {
            outline.enabled = true;
        }
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);

        if (outline != null)
        {
            outline.enabled = false;
        }
    }

}
