using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class CulturingResult
{
    public string microbeName;
    public GameObject resultModel;
}

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

    [Header("애니메이션 설정")]
    [Tooltip("액체가 고체로 굳는 데 걸리는 시간(초)")]
    public float solidificationTime = 5.0f;

    [Tooltip("액체가 페트리 접시에 차오르는 시간(초)")]
    public float fillDuration = 1.5f;

    [Tooltip("미생물 방울이 차오르는 시간(초)")]
    public float inoculationFillDuration = 0.3f;

    [Tooltip("미생물이 펴지는 시간(초)")]
    public float spreadAnimationDuration = 5.0f;

    [Header("최종 결과물 설정")]
    [Tooltip("연결 가능한 결과물 목록")]
    public CulturingResult[] possibleResults;

    [Header("시각 UI")]
    public DynamicInfoUI_G infoPanel;

    [SerializeField] private ToolType toolType = ToolType.Tray;
    private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();
    private Material liquidMaterial;
    private Material inoculationMaterial;
    private bool isInCleanBench = false;

    public bool IsWritable { get => currentState == DishState.Empty || currentState == DishState.Solid; set { } }
    public ToolType ToolType { get => toolType; set => toolType = value; }


    private void OnEnable()
    {
        C_ExperimentDataParser.I.DataParsed.AddListener(OnDataParsed);
    }

    private void OnDisable()
    {
        C_ExperimentDataParser.I.DataParsed.RemoveListener(OnDataParsed);
    }

    private void OnDataParsed(ParseEventArgs e)
    {
        if (e.toTool == this)
        {
            ImportLiquidData(e.fromTool.ExportLiquidDatas());
        }
    }

    private void Start()
    {
        if (liquidVisual != null)
            liquidMaterial = liquidVisual.GetComponent<Renderer>().material;

        if (inoculationVisual != null)
            inoculationMaterial = inoculationVisual.GetComponent<Renderer>().material;

        liquidMaterial.SetFloat("_Fill", -0.005f);
        inoculationMaterial.SetFloat("_Fill", -0.0005f);

        UpdateInfoPanel();
        UpdateVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CleanBenchTrigger_G>() != null)
        {
            isInCleanBench = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CleanBenchTrigger_G>() != null)
        {
            isInCleanBench = false;
        }
    }

    public void ImportLiquidData(List<LiquidData_L> receivedDatas)
    {
        if (!isInCleanBench)
        {
            UIManager_G.Instance.ShowWarningMessage("경고! 작업은 클린벤치 안에서 진행해주세요.");
            return;
        }

        switch (currentState)
        {
            case DishState.Empty:
                bool hasAgar = receivedDatas.Exists(data => data.type == PourableType.Agar);
                if (hasAgar)
                {
                    liquidDatas.AddRange(receivedDatas);
                    currentState = DishState.Liquid;
                    UpdateVisuals();
                    StartCoroutine(FillRoutine());
                }
                break;

            case DishState.Solid:
                bool hasAgarInoculation = receivedDatas.Exists(data => data.type == PourableType.Agar);
                if (!hasAgarInoculation && receivedDatas.Count > 0)
                {
                    StartCoroutine(InoculateRoutine());
                }
                break;
        }
    }

    public void CompleteSpreading()
    {
        if (!isInCleanBench)
        {
            UIManager_G.Instance.ShowWarningMessage("경고! 도말 작업은 클린벤치 안에서 진행해주세요.");
            return;
        }

        if (currentState == DishState.Inoculated)
        {
            StartCoroutine(SpreadRoutine());
        }

        UpdateInfoPanel();
    }

    private IEnumerator InoculateRoutine()
    {
        if (inoculationVisual == null) yield break;

        inoculationVisual.SetActive(true);
        inoculationVisual.transform.localScale = new Vector3(0.004f, inoculationVisual.transform.localScale.y, 0.004f);

        float elapsedTime = 0f;
        float startFill = -0.0005f;
        float endFill = 0f;

        if (inoculationMaterial != null)
            inoculationMaterial.SetFloat("_Fill", startFill);

        while (elapsedTime < inoculationFillDuration)
        {
            elapsedTime += Time.deltaTime;
            float newFillAmount = Mathf.Lerp(startFill, endFill, elapsedTime / inoculationFillDuration);

            if (inoculationMaterial != null)
                inoculationMaterial.SetFloat("_Fill", newFillAmount);

            yield return null;
        }

        if (inoculationMaterial != null)
            inoculationMaterial.SetFloat("_Fill", endFill);

        currentState = DishState.Inoculated;
        UpdateInfoPanel();
    }

    private IEnumerator SpreadRoutine()
    {
        if (inoculationVisual == null) yield break;

        Debug.Log("도말 작업 애니메이션 시작.");

        float elapsedTime = 0f;
        Vector3 startScale = inoculationVisual.transform.localScale;
        Vector3 endScale = new Vector3(0.015f, startScale.y, 0.015f);

        while (elapsedTime < spreadAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            inoculationVisual.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / spreadAnimationDuration);
            
            yield return null;
        }

        inoculationVisual.transform.localScale = endScale;

        if (inoculationVisual != null) 
            inoculationVisual.SetActive(false);

        currentState = DishState.Spread;
        UpdateVisuals();
        UpdateInfoPanel();
        Debug.Log("도말 작업이 완료되었습니다.");
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        return this.liquidDatas;
    }

    public void ShowResult()
    {
        // 1. 이 페트리 접시에 어떤 미생물이 사용되었는지 찾습니다.
        LiquidData_L usedMicrobe = liquidDatas.Find(data => data.type == PourableType.Microbe);

        // 2. 만약 미생물 데이터가 없다면, 함수를 종료합니다.
        if (usedMicrobe == null)
        {
            Debug.LogWarning("결과를 표시할 미생물 데이터가 없습니다.");
            return;
        }

        // 3. 사용된 미생물 이름과 일치하는 결과물을 목록에서 찾습니다.
        foreach (var result in possibleResults)
        {
            if (result.microbeName == usedMicrobe.liquidName)
            {
                if (result.resultModel != null)
                {
                    if (solidVisual != null) 
                        solidVisual.SetActive(false);

                    if (inoculationVisual != null) 
                        inoculationVisual.SetActive(false);

                    result.resultModel.SetActive(true);
                    Debug.Log(result.microbeName + "에 대한 결과물을 표시합니다.");
                }
                return;
            }
        }
    }

    public void ClearData()
    {
        this.liquidDatas.Clear();
        currentState = DishState.Empty;
        UpdateVisuals();

        UpdateInfoPanel();

        foreach (var result in possibleResults)
        {
            if (result.resultModel != null)
            {
                result.resultModel.SetActive(false);
            }
        }
    }

    private IEnumerator FillRoutine()
    {
        currentState = DishState.Liquid;
        if (liquidVisual != null) liquidVisual.SetActive(true);

        float elapsedTime = 0f;
        float startFill = -0.005f;
        float endFill = 0f;

        if (liquidMaterial != null)
            liquidMaterial.SetFloat("_Fill", startFill);

        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            float newFillAmount = Mathf.Lerp(startFill, endFill, elapsedTime / fillDuration);

            if (liquidMaterial != null)
                liquidMaterial.SetFloat("_Fill", newFillAmount);

            yield return null;
        }

        if (liquidMaterial != null)
            liquidMaterial.SetFloat("_Fill", endFill);

        StartCoroutine(SolidifyRoutine());
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

    public void OnGrab()
    {
        if (infoPanel != null)
            infoPanel.gameObject.SetActive(true);
    }

    public void OnRelease()
    {
        if (infoPanel != null)
            infoPanel.gameObject.SetActive(false);
    }

    private void UpdateInfoPanel()
    {
        if (infoPanel == null) return;

        string currentStateInfo = currentState.ToString();
        infoPanel.UpdateInfo(currentStateInfo);
    }
}
