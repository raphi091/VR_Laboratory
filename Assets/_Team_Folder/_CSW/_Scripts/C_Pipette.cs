using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class C_Pipette : MonoBehaviour, C_ExperimentProp
{
    [SerializeField] private LiquidData_L liquidData;
    [SerializeField] private InputActionReference rightPippetPush;
    [SerializeField] private InputActionReference leftPippetPush;
    
    public LiquidData_L LiquidData { get => liquidData; set => liquidData = value; }
    
    private bool isSelected = false;
    
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelected = true;
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        isSelected = false;
    }
    
    public void ImportLiquidData(LiquidData_L liquidData)
    {
        this.liquidData = liquidData;
    }
    
    public LiquidData_L ExportLiquidData()
    {
        return liquidData;
    }
    
    public LiquidData_L ExportLiquidData(LiquidData_L liquidData)
    {
        return null;
    }

    public List<LiquidData_L> ExportLiquidDataList()
    {
        return null;
    }
}
