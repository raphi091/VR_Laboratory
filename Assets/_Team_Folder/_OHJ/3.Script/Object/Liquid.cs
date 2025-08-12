using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Liquid : MonoBehaviour
{
    public Renderer rend;
    public float fillAmount  = 1f;
    public float currentAmount = -0.3f;

    private void Start()
    {
        currentAmount = -0.3f;
        rend = GetComponent<Renderer>();
    }

    private void Update()
    {
        currentAmount += fillAmount * Time.deltaTime;
        rend.material.SetFloat("_Fill", currentAmount);

        if(currentAmount > 1f)
        {
            currentAmount = 1f;
        }
    }
}
