using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum ToolType
{
    None=0,
    Flask=1,
    Pippet=2,
    Tray=3
}

public class C_ExperimentDataParser : Ch_BehaviourSingleton<C_ExperimentDataParser>
{
    protected override bool IsDontdestroy()
    {
        return true;
    }
    
    public ParseEventArgs ParseEventArgs;
    
    public UnityEvent<ParseEventArgs> DataParsed=new UnityEvent<ParseEventArgs>();
}

public interface C_ExperimentTool
{
    public bool IsWritable { get; set; }
    public ToolType ToolType { get; set; }
    public void ImportLiquidData(List<LiquidData_L> liquidDatas);
    public List<LiquidData_L> ExportLiquidDatas();
    public void ClearData();
}

public class ParseEventArgs : EventArgs
{
    public C_ExperimentTool fromTool;
    public C_ExperimentTool toTool;
}
