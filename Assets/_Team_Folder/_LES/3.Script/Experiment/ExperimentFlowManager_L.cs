using UnityEngine;

public class ExperimentFlowManager_L : MonoBehaviour
{
    [Header("PCR Experiment Equipment")]
    public GameObject thermocycler;
    public GameObject gelElectrophoresis;
    public GameObject gelDoc;

    public LabEquipmentController_L gelElectrophoresisController;
    public GelDocScreenController_L gelDocScreenController;

    [Header("Culturing Experiment Equipment")]
    public GameObject autoclave;
    public GameObject shakingIncubator;
    public GameObject airIncubator;

    [Header("Result Objects")]
    public GameObject finalPetriDish;

    void Start()
    {
        Debug.Log("--- ExperimentFlowManager: 시작 ---");

        if (GameStateManager_L.Instance != null && GameStateManager_L.Instance.IsCulturingOvernight)
        {
            Debug.Log("GameStateManager에서 '밤샘 배양' 상태 확인. 결과 표시를 시도합니다.");
            ShowPetriDishResult();
            GameStateManager_L.Instance.IsCulturingOvernight = false;
        }

        Debug.Log("초기 장비 상태를 설정합니다.");
        SetEquipmentActive(thermocycler, true);
        SetEquipmentActive(gelElectrophoresis, false);
        SetEquipmentActive(gelDoc, false);
        SetEquipmentActive(autoclave, true);
        SetEquipmentActive(shakingIncubator, false);
        SetEquipmentActive(airIncubator, false);
    }

    public void OnThermocyclerFinished()
    {
        Debug.Log(">> 이벤트 수신: Thermocycler 완료. Gel Electrophoresis 활성화.");
        SetEquipmentActive(gelElectrophoresis, true);
    }

    public void OnGelElectrophoresisFinished()
    {
        Debug.Log(">> 이벤트 수신: Gel Electrophoresis 완료. Gel Doc 활성화.");
        SetEquipmentActive(gelDoc, true);
    }

    public void OnAutoclaveFinished()
    {
        Debug.Log(">> 이벤트 수신: Autoclave 완료. Shaking Incubator 활성화.");
        SetEquipmentActive(shakingIncubator, true);
    }

    public void OnShakingIncubatorFinished()
    {
        Debug.Log(">> 이벤트 수신: Shaking Incubator 완료. Air Incubator 활성화.");
        SetEquipmentActive(airIncubator, true);
    }

    public void OnAirIncubatorStarted()
    {
        Debug.Log(">> 이벤트 수신: Air Incubator 시작. '밤샘 배양' 상태를 GameStateManager에 저장합니다.");
        if (GameStateManager_L.Instance != null)
        {
            GameStateManager_L.Instance.IsCulturingOvernight = true;
        }
    }

    private void ShowPetriDishResult()
    {
        if (finalPetriDish == null)
        {
            Debug.LogError("오류: finalPetriDish 변수가 인스펙터에 할당되지 않았습니다!");
            return;
        }

        Renderer renderer = finalPetriDish.GetComponentInChildren<Renderer>();
        if (renderer != null && ResultManager_L.Instance != null)
        {
            renderer.material.mainTexture = ResultManager_L.Instance.GetRandomCulturingResult();
            Debug.Log("성공: 밤샘 배양된 페트리 접시 결과 이미지를 적용했습니다.");
        }
        else
        {
            Debug.LogError("오류: finalPetriDish에서 Renderer 컴포넌트를 찾지 못했거나 ResultManager가 없습니다.");
        }
    }

    private void SetEquipmentActive(GameObject equipment, bool isActive)
    {
        if (equipment == null) return;

        var controller = equipment.GetComponent<LabEquipmentController_L>();
        if (controller != null)
        {
            controller.enabled = isActive;
        }

        var canvasGroup = equipment.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = isActive;
            canvasGroup.alpha = isActive ? 1.0f : 0.3f;
        }
        Debug.Log($"장비 '{equipment.name}' 상태 변경: {(isActive ? "활성화" : "비활성화")}");
    }
    
    // GelElectrophoresis가 '작동 시작' 신호를 보내면 호출됩니다.
    public void OnGelElectrophoresisProcessStarted()
    {
        Debug.Log(">> 매니저: GelElectrophoresis 작동 시작 감지. GelDoc에 결과 표시를 명령합니다.");
        if (ResultManager_L.Instance != null && gelDocScreenController != null)
        {
            Texture resultToShow = ResultManager_L.Instance.GetRandomPcrResult();
            gelDocScreenController.StartAnalysis();
        }
    }

    // GelDoc이 '보기 중단' 신호를 보내면 호출됩니다.
    public void OnGelDocViewingStopped()
    {
        Debug.Log(">> 매니저: GelDoc 보기 중단 감지. GelElectrophoresis에 아이템 배출을 명령합니다.");
        if (gelElectrophoresisController != null)
        {
            gelElectrophoresisController.MakeItemAvailable();
        }
    }
}