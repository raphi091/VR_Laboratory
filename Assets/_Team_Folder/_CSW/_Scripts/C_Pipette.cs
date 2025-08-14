using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class C_Pipette : MonoBehaviour, C_ExperimentTool
{
    [SerializeField] private List<LiquidData_L> liquidDatas;
    [SerializeField] private InputActionReference rightPippetPush;
    [SerializeField] private InputActionReference leftPippetPush;
    [SerializeField] private ToolType toolType=ToolType.None;
    [SerializeField] private bool isWritable = false;
    
    public List<LiquidData_L> LiquidDatas { get => liquidDatas; set => liquidDatas = value; }
    public ToolType ToolType { get => toolType; set => toolType = value; }
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
        if (e.toTool == this&&e.fromTool.ToolType==ToolType.Flask)
        {
            if (IsWritable)
            {
                ImportLiquidData(e.fromTool.ExportLiquidDatas());
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
