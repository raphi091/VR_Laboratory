using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum propType
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
}

public interface C_ExperimentProp
{
    public void ImportLiquidData(LiquidData_L liquidData);
    
    public LiquidData_L ExportLiquidData();
    public LiquidData_L ExportLiquidData(LiquidData_L liquidData);
    
    public List<LiquidData_L> ExportLiquidDataList();
}
