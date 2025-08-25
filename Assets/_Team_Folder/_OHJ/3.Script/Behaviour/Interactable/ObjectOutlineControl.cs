using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectOutlineControl : MonoBehaviour
{
    [SerializeField] private InteractionEventHandler eventHandler;
    [SerializeField] private Outline outline;

    private void Awake()
    {
        if(TryGetComponent(out outline))
        {
            outline.enabled = false;
        }
        TryGetComponent(out eventHandler);
    }

    private void OnEnable()
    {
        eventHandler.OnHoverCheck += SetActiveOutline;
    }

    private void OnDisable()
    {
        eventHandler.OnHoverCheck -= SetActiveOutline;
    }

    public void SetActiveOutline(bool on)
    {
        if (on)
        {
            outline.enabled = true;
        }

        else
        {
            outline.enabled = false;
        }
    }
}
