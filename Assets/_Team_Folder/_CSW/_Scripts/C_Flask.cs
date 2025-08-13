using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_Flask : MonoBehaviour, C_ExperimentProp
{
    [SerializeField] private List<LiquidData_L> liquidDatas=new List<LiquidData_L>();
    
    private Ch_VelocityInteractable velocityInteractable;
    private bool combineFailed = false;
    private bool isWritable = false;
    
    public void ImportLiquidData(LiquidData_L liquidData)
    {
        this.liquidDatas.Add(liquidData);
    }
    
    public LiquidData_L ExportLiquidData()
    {
        return null;
    }
    public LiquidData_L ExportLiquidData(LiquidData_L liquidData)
    {
        if (liquidDatas.Contains(liquidData))
        {
            return liquidData;
        }
        return null;
    }
    
    public List<LiquidData_L> ExportLiquidDataList()
    {
        return liquidDatas;
    }
}
