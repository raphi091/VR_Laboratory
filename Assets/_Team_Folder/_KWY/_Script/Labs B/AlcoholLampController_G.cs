using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AlcoholLampController_G : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("불꽃 VFX")]
    public VisualEffect flameVFX;

    [Tooltip("알코올 액체 부분의 Renderer")]
    public Renderer alcoholLiquidRenderer;

    [Header("상태")]
    [Tooltip("현재 램프가 켜져 있는지 여부")]
    public bool isLit = false;

    [Tooltip("불이 붙는데 걸리는 시간")]
    public float burnTime = 2f;

    [Tooltip("연료가 모두 소진될 때까지 걸리는 총 시간(초)")]
    public float totalBurnDuration = 60f;

    private bool isBurning = false;
    private Material alcoholMaterialInstance;

    private void Start()
    {
        if (flameVFX != null)
        {
            flameVFX.Stop();
            flameVFX.gameObject.SetActive(false);
        }

        isLit = false;

        if (alcoholLiquidRenderer != null)
        {
            alcoholMaterialInstance = alcoholLiquidRenderer.material;
            alcoholMaterialInstance.SetFloat("_Fill", 0.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLit || isBurning) return;

        TorchController_G torch = other.GetComponentInParent<TorchController_G>();

        if (torch != null && torch.isLit)
        {
            StartCoroutine(LightLamp());
        }
    }

    private IEnumerator LightLamp()
    {
        isBurning = true;

        yield return new WaitForSeconds(burnTime);

        if (flameVFX != null)
        {
            flameVFX.gameObject.SetActive(true);
            flameVFX.SendEvent("OnPlay");
        }

        isLit = true;
        isBurning = false;

        StartCoroutine(BurnDownRoutine());
    }

    private IEnumerator BurnDownRoutine()
    {
        Debug.Log("알코올 램프 연소를 시작합니다.");
        float elapsedTime = 0f;
        float startFill = 0.02f;
        float endFill = -0.015f;

        while (elapsedTime < totalBurnDuration)
        {
            if (!isLit)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float newFillAmount = Mathf.Lerp(startFill, endFill, elapsedTime / totalBurnDuration);

            if (alcoholMaterialInstance != null)
            {
                alcoholMaterialInstance.SetFloat("_Fill", newFillAmount);
            }

            yield return null;
        }

        ExtinguishLamp();
    }

    public void ExtinguishLamp()
    {
        if (!isLit) return;

        Debug.Log("연료가 모두 소진되어 불이 꺼집니다.");
        if (flameVFX != null)
        {
            flameVFX.SendEvent("OnStop");
        }
        isLit = false;
    }
}
