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

    private Coroutine fillCoroutine;
    private Coroutine ChangecolorCoroutine;

    private void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;   
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
        float newAmount = currentAmount + fillAmount;

        if(newAmount > targetAmount)
        {
            newAmount = targetAmount;
        }

        if(fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        fillCoroutine = StartCoroutine(FillAnimation(currentAmount, newAmount, 0.5f));
        //if(currentAmount != targetAmount)
        //{
        //    currentAmount = Mathf.Lerp(currentAmount, targetAmount, fillAmount * Time.deltaTime);
        //}

        //currentAmount += fillAmount;

        //if (currentAmount > 0.8f)
        //{
        //    currentAmount = 0.8f;
        //}

        //if(mat.HasProperty("_Fill"))
        //{
        //    mat.SetFloat("_Fill", currentAmount);
        //}
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
