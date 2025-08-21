using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플라스크에 sample을 넣을 시 조금씩 차오르도록 구현
public class Liquid_O : MonoBehaviour
{
    public Material mat;
    public float fillAmount  = 1f;
    public float currentAmount;

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
        currentAmount += fillAmount;

        if (currentAmount > 1f)
        {
            currentAmount = 1f;
        }

        if(mat.HasProperty("_Fill"))
        {
            mat.SetFloat("_Fill", currentAmount);
        }
    }
}
