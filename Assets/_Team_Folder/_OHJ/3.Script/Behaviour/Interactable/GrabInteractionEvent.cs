using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabInteractionEvent : InteractionEventHandler
{
    private XRRayInteractor rayInteractor = null;

    void Start()
    {
        rayInteractor = interactor as XRRayInteractor;
    }

    protected override void OnHoverEnter(HoverEnterEventArgs args)
    {
        base.OnHoverEnter(args);

        rayInteractor = interactor as XRRayInteractor;

        if (rayInteractor == null) return;

        if (rayInteractor.TryGetHitInfo(out Vector3 hitPoint, out Vector3 hitNormal, out int _, out bool _))
        {
            OnHoverCheck?.Invoke(true);
        }
    }

    protected override void OnHoverExit(HoverExitEventArgs args)
    {
        base.OnHoverExit(args);

        if (handCount > 0) return;

        rayInteractor = null;
        OnHoverCheck?.Invoke(false);
    }
}