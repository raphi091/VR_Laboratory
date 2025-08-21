using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LiquidSource_G : MonoBehaviour, C_ExperimentTool
{
    [Header("내용물 정보")]
    [Tooltip("액체/재료의 데이터 애셋")]
    public LiquidData_L liquidDataAsset;

    [Header("모델 설정")]
    [Tooltip("닫힌 상태의 튜브 모델")]
    public GameObject closedModel;

    [Tooltip("열린 상태의 튜브 모델")]
    public GameObject openModel;

    [SerializeField] private bool isWritable = false;
    [SerializeField] private ToolType toolType = ToolType.None; 

    public bool IsWritable { get => isWritable; set => isWritable = value; }
    public ToolType ToolType { get => toolType; set => toolType = value; }

    private void Start()
    {
        if (closedModel != null) 
            closedModel.SetActive(true);

        if (openModel != null) 
            openModel.SetActive(false);
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        if (liquidDataAsset != null)
        {
            return new List<LiquidData_L> { liquidDataAsset };
        }

        return new List<LiquidData_L>();
    }

    public void ClearData()
    {
        gameObject.SetActive(false);
    }

    public void ImportLiquidData(List<LiquidData_L> liquidDatas) 
    { 
    }
}
