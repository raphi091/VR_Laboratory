using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플라스크에 sample을 넣을 시 조금씩 차오르도록 구현
public class Liquid_O : MonoBehaviour
{
    public Material mat;
    public float fillAmount  = 1f;
    public float currentAmount;
    public float targetAmount = 0.5f;

    [Header("플라스크")]
    private SampleFlask_O flask;
    public LiquidData_L TBE;
    public LiquidData_L Agarose;

    private Coroutine fillCoroutine;
    private Coroutine ChangecolorCoroutine;

    private void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
        flask = GetComponentInParent<SampleFlask_O>();
    }

    private void Start()
    {
        if(mat != null && mat.HasProperty("_Fill"))
        {
            mat.SetFloat("_Fill", currentAmount);
        }
    }

    public void FillLiquid()
    {
        float gelFillAmount = fillAmount;   // 채우기 기본값을 저장
        // 겔 플라스크라면
        // TBE랑 아가로스의 양이 서로 다르게 나오도록 한다
        if (flask.isGel)
        {
            if (flask.receiveddLiquids.Contains(TBE))
            {
                gelFillAmount = 0.51f;
            }
        }

        float newAmount = currentAmount + gelFillAmount;
        Debug.LogWarning($"{gelFillAmount}만큼 채웠습니다");

        if (newAmount > targetAmount)
        {
            newAmount = targetAmount;
        }

        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        fillCoroutine = StartCoroutine(FillAnimation(currentAmount, newAmount, 0.5f));

    }

    private IEnumerator FillAnimation(float from, float to, float duration)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAmount = Mathf.Lerp(from, to, elapsed / duration);

            if (mat.HasProperty("_Fill"))
            {
                mat.SetFloat("_Fill", currentAmount);
            }

            yield return null;
        }
        currentAmount = to;
        mat.SetFloat("_Fill", currentAmount);
    }

    public void ChangeLiquidColor(Color targetColor, float duration)
    {
        if(ChangecolorCoroutine != null)
        {
            StopCoroutine(ChangecolorCoroutine);
        }
        ChangecolorCoroutine = StartCoroutine(ChangeLiquidColor_co(targetColor, duration));
        Debug.Log("색깔바꾸기 코루틴 시작");
    }

    private IEnumerator ChangeLiquidColor_co(Color targetColor, float duration)
    {
        if(mat == null || !mat.HasProperty("_FresnelColor"))
        {
            yield break;
        }

        Color startColor = mat.GetColor("_FresnelColor");
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color changeColor = Color.Lerp(startColor, targetColor, elapsed / duration);
            mat.SetColor("_FresnelColor", changeColor);
            yield return null;
        }

        mat.SetColor("_FresnelColor", targetColor);
    }
}
