using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class C_Flask : MonoBehaviour, C_ExperimentTool
{
    [SerializeField] private List<LiquidData_L> liquidDatas=new List<LiquidData_L>();
    [SerializeField] private ToolType toolType=ToolType.None;
    
    public List<LiquidData_L> LiquidDatas { get => liquidDatas; set => liquidDatas = value; }
    public ToolType ToolType { get => toolType; set => toolType = value; }
    
    private Ch_VelocityInteractable velocityInteractable;
    private bool combineFailed = false;
    private bool isWritable = false;
    public bool IsWritable { get => isWritable; set => isWritable = value; }
    
    void OnEnable()
    {
        C_ExperimentDataParser.I.DataParsed.AddListener(OnDataParsed);
    }

    void OnDisable()
    {
        C_ExperimentDataParser.I.DataParsed.RemoveListener(OnDataParsed);
    }

    void OnDataParsed(ParseEventArgs e)
    {
        if (e.toTool == this)
        {
            if (IsWritable)
            {
                ImportLiquidData(e.fromTool.ExportLiquidDatas());
                if (e.fromTool.ToolType == ToolType.Pippet)
                {
                    e.fromTool.ClearData();
                }
            }
        }
    }
    
    public void ImportLiquidData(List<LiquidData_L> liquidData)
    {
        this.liquidDatas.AddRange(liquidDatas);
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        return liquidDatas;
    }
    
    public void ClearData()
    {
        liquidDatas.Clear();
    }
    

}
