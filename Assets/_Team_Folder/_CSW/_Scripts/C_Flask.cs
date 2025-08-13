using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_Flask : MonoBehaviour
{
    [SerializeField] private List<LiquidData_L> liquidData=new List<LiquidData_L>();
    
    private Ch_VelocityInteractable velocityInteractable;
    private bool combineFailed = false;
}
