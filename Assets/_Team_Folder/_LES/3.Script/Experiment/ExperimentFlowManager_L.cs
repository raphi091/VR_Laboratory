using UnityEngine;

public class ExperimentFlowManager_L : MonoBehaviour
{
    [Header("PCR Experiment Equipment")]
    // 모든 변수를 GameObject 대신 스크립트 컴포넌트로 직접 받도록 통일합니다.
    public LabEquipmentController_L thermocyclerController;
    public LabEquipmentController_L gelElectrophoresisController;
    public GelDocScreenController_L gelDocScreenController;

    [Header("Culturing Experiment Equipment")]
    public LabEquipmentController_L autoclaveController;
    public LabEquipmentController_L shakingIncubatorController;
    public LabEquipmentController_L airIncubatorController;

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
        SetEquipmentLockState(thermocyclerController, false); // false = 잠금 해제
        SetEquipmentLockState(gelElectrophoresisController, true); // true = 잠김

        if (gelDocScreenController != null)
        {
            gelDocScreenController.SetLockState(true); // true = 잠김
        }
        SetEquipmentLockState(autoclaveController, false);
        SetEquipmentLockState(shakingIncubatorController, true);

        bool airIncubatorLocked = (GameStateManager_L.Instance == null || !GameStateManager_L.Instance.IsCulturingOvernight);
        SetEquipmentLockState(airIncubatorController, airIncubatorLocked);
    }

    public void OnThermocyclerFinished()
    {
        Debug.Log(">> 이벤트 수신: Thermocycler 완료. Gel Electrophoresis 잠금 해제.");
        SetEquipmentLockState(gelElectrophoresisController, false);
    }

    public void OnAutoclaveFinished()
    {
        Debug.Log(">> 이벤트 수신: Autoclave 완료. Shaking Incubator 잠금 해제.");
        SetEquipmentLockState(shakingIncubatorController, false);
    }

    public void OnShakingIncubatorFinished()
    {
        Debug.Log(">> 이벤트 수신: Shaking Incubator 완료. Air Incubator 잠금 해제.");
        SetEquipmentLockState(airIncubatorController, false);
    }

    public void OnGelElectrophoresisFinished()
    {
        Debug.Log(">> 이벤트 수신: Gel Electrophoresis 완료. Gel Doc 활성화.");
        if (gelDocScreenController != null)
        {
            gelDocScreenController.SetLockState(false); // false = 잠금 해제
        }
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

    private void SetEquipmentLockState(LabEquipmentController_L controller, bool isLocked)
    {
        if (controller == null) return;

        controller.SetLockState(isLocked);

        var collider = controller.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        var canvasGroup = controller.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = !isLocked;
            canvasGroup.alpha = isLocked ? 0.3f : 1.0f;
        }
        Debug.Log($"장비 '{controller.name}' 상태 변경: {(isLocked ? "잠김" : "활성화")}");
    }

    public void OnGelElectrophoresisProcessStarted()
    {
        Debug.Log(">> 매니저: GelElectrophoresis 작동 시작 감지. GelDoc에 분석 시작을 명령합니다.");
        if (gelDocScreenController != null)
        {
            // GelDoc의 잠금을 해제하고, 활성화한 뒤, 분석을 시작시킵니다.
            gelDocScreenController.gameObject.SetActive(true); // 혹시 모르니 활성화
            gelDocScreenController.SetLockState(false);      // << 잠금 해제 (핵심 수정!)
            gelDocScreenController.StartAnalysis();          // 분석 시작
        }
    }

    public void OnGelDocViewingStopped()
    {
        Debug.Log(">> 매니저: GelDoc 보기 중단 감지. GelElectrophoresis에 아이템 배출을 명령합니다.");
        if (gelElectrophoresisController != null)
        {
            gelElectrophoresisController.MakeItemAvailable();
        }
    }
}