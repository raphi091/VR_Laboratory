using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PetriDishController_G : MonoBehaviour, C_ExperimentTool
{
    public enum DishState
    {
        Empty,
        Liquid,
        Solid,
        Inoculated,
        Spread
    }

    [Header("상태 및 시각 효과")]
    [Tooltip("현재 페트리 접시의 상태")]
    public DishState currentState = DishState.Empty;

    [Tooltip("액체 상태일 때 보여줄 오브젝트")]
    public GameObject liquidVisual;

    [Tooltip("고체 상태일 때 보여줄 오브젝트")]
    public GameObject solidVisual;

    [Tooltip("미생물 접종 시 보여줄 작은 액체 방울 오브젝트")]
    public GameObject inoculationVisual;

    [Tooltip("액체가 고체로 굳는 데 걸리는 시간(초)")]
    public float solidificationTime = 5.0f;

    [SerializeField] private ToolType toolType = ToolType.Tray;
    private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();

    public bool IsWritable { get => currentState == DishState.Empty || currentState == DishState.Solid; set { } }
    public ToolType ToolType { get => toolType; set => toolType = value; }


    private void Start()
    {
        UpdateVisuals();
    }

    public void ImportLiquidData(List<LiquidData_L> receivedDatas)
    {
        switch (currentState)
        {
            case DishState.Empty:
                bool hasAgar = receivedDatas.Exists(data => data.type == PourableType.Agar);
                if (hasAgar)
                {
                    liquidDatas.AddRange(receivedDatas);
                    currentState = DishState.Liquid;
                    UpdateVisuals();
                    StartCoroutine(SolidifyRoutine());
                }
                break;

            case DishState.Solid:
                bool hasAgarInoculation = receivedDatas.Exists(data => data.type == PourableType.Agar);
                if (!hasAgarInoculation && receivedDatas.Count > 0)
                {
                    currentState = DishState.Inoculated;
                    UpdateVisuals();
                }
                break;
        }
    }

    public void CompleteSpreading()
    {
        if (currentState == DishState.Inoculated)
        {
            currentState = DishState.Spread;
            UpdateVisuals();
            Debug.Log("도말 작업이 완료되었습니다.");
        }
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        return this.liquidDatas;
    }

    public void ClearData()
    {
        this.liquidDatas.Clear();
        currentState = DishState.Empty;
        UpdateVisuals();
    }

    private IEnumerator SolidifyRoutine()
    {
        Debug.Log("배지가 굳기 시작합니다...");
        yield return new WaitForSeconds(solidificationTime);

        currentState = DishState.Solid;
        UpdateVisuals();
        Debug.Log("배지가 모두 굳었습니다!");
    }

    private void UpdateVisuals()
    {
        if (liquidVisual != null) 
            liquidVisual.SetActive(currentState == DishState.Liquid);

        if (solidVisual != null) 
            solidVisual.SetActive(currentState == DishState.Solid || currentState == DishState.Inoculated || currentState == DishState.Spread);

        if (inoculationVisual != null) 
            inoculationVisual.SetActive(currentState == DishState.Inoculated);
    }
}
