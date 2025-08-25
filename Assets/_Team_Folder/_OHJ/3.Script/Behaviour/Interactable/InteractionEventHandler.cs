using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public abstract class InteractionEventHandler : MonoBehaviour
{
    protected int handCount = 0;
    protected XRBaseInteractable interactable;
    public XRBaseInteractor interactor;

    public UnityAction<bool> OnHoverCheck;
    public UnityAction<Vector3> OnHoverPointUpdate;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if(interactable == null)
        {
            interactable = GetComponentInChildren<XRBaseInteractable>();
        }
    }

    void OnEnable()
    {
        if(interactable == null)
        {
            Debug.LogWarning($"{name}] XRBaseInteractable ´©¶ôµÊ!");
            return;
        }

        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);

        //try
        //{
        //    interactable.hoverEntered.AddListener(OnHoverEnter);
        //    interactable.hoverExited.AddListener(OnHoverExit);
        //}
        //catch
        //{
            
        //}
        
    }

    void OnDisable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        //try
        //{
        //    interactable.hoverEntered.RemoveListener(OnHoverEnter);
        //    interactable.hoverExited.RemoveListener(OnHoverExit);
        //}
        //catch
        //{

        //}
        
    }

    protected virtual void OnHoverEnter(HoverEnterEventArgs args)
    {
        handCount++;
        this.interactor = args.interactorObject as XRBaseInteractor;
    }

    protected virtual void OnHoverExit(HoverExitEventArgs args)
    {
        handCount--;
        interactable = null;
    }
}