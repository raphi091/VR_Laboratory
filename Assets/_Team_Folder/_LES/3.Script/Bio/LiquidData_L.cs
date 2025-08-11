using UnityEngine;

[CreateAssetMenu(fileName = "NewLiquidData_L", menuName = "Biology Simulator/Liqui")]
public class LiquidData_L : ScriptableObject
{
    [Header("기본정보")]
    public string liquidName;
    public string formula;

    [TextArea]
    public string description;

    [Header("시각 정보")]
    public string colorName;
    public Color color;
}