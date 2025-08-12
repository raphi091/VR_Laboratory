using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Ch_VelocityInteractable : XRGrabInteractable
{
    private Ch_ContollerVelocity controllerVelocity;
    public Vector3 velocity;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        controllerVelocity=args.interactorObject.transform.gameObject.GetComponent<Ch_ContollerVelocity>();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        controllerVelocity = null;
        velocity = Vector3.zero;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (isSelected)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                ApplyVelocity();
            }
        }
    }

    private void ApplyVelocity()
    {
        velocity = controllerVelocity? controllerVelocity.velocity : Vector3.zero;
    }
}
