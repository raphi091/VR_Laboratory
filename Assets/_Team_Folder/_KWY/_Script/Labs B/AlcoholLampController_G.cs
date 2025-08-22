using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AlcoholLampController_G : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("불꽃 VFX")]
    public VisualEffect flameVFX;

    [Header("상태")]
    [Tooltip("현재 램프가 켜져 있는지 여부")]
    public bool isLit = false;

    void Start()
    {
        if (flameVFX != null)
        {
            flameVFX.Stop();
            flameVFX.gameObject.SetActive(false);
        }

        isLit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLit) return;

        TorchController_G torch = other.GetComponentInParent<TorchController_G>();

        if (torch != null && torch.isLit)
        {
            LightLamp();
        }
    }

    public void LightLamp()
    {
        if (flameVFX != null)
        {
            flameVFX.gameObject.SetActive(true);
            flameVFX.SendEvent("OnPlay");
        }

        isLit = true;
    }
}
