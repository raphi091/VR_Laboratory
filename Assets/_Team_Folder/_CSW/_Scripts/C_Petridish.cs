using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_Petridish : MonoBehaviour
{
    [SerializeField] List<LiquidData_L> liquidData;
    bool combineFailed = false;


    private void OnParticleCollision(GameObject other)
    {
        C_Pipette pipette = other.GetComponent<C_Pipette>();
        if (other.CompareTag("Pippet"))
        {
            if (liquidData.Contains(pipette.LiquidData))
            {
                
            }
            else
            {
                combineFailed = true;
            }
        }
    }
}
