using UnityEngine.XR.Interaction.Toolkit;

public interface IInteractable
{
    public abstract void OnHoverEnter();
    public abstract void OnHoverExit();

    // 매개 변수는 상호작용하는
    public abstract void PerForm(XRBaseInteractor interactor);
}