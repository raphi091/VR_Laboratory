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

    private FlaskLiquidController_G _G;

    void Start()
    {
//        Debug.Log("--- ExperimentFlowManager: 시작 ---");

        if (GameStateManager_L.Instance != null && GameStateManager_L.Instance.IsCulturingOvernight)
        {
            Debug.Log("GameStateManager에서 '밤샘 배양' 상태 확인. 결과 표시를 시도합니다.");
            ShowPetriDishResult(); // 수정된 함수 호출
        }

//        Debug.Log("초기 장비 상태를 설정합니다.");
        SetEquipmentLockState(thermocyclerController, false); // false = 잠금 해제
        SetEquipmentLockState(gelElectrophoresisController, true); // true = 잠김

        if (gelDocScreenController != null)
        {
            gelDocScreenController.SetLockState(true); // true = 잠김
        }
        SetEquipmentLockState(autoclaveController, false);
        SetEquipmentLockState(shakingIncubatorController, true);

        bool airIncubatorLocked = (C_DataManager.I.gameData == null || !C_DataManager.I.gameData.IsCulturingOvernight);
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
        if (GameStateManager_L.Instance != null && airIncubatorController != null)
        {
            GameObject processingItem = airIncubatorController.GetCurrentlyProcessingItem();

            if (processingItem != null)
            {
                ExperimentItem_L itemInfo = processingItem.GetComponent<ExperimentItem_L>();
                if (itemInfo != null)
                {
                    GameStateManager_L.Instance.IsCulturingOvernight = true;
                    GameStateManager_L.Instance.IncubatedPetriDishID = itemInfo.uniqueId;
                }
            }
        }
    }

    private void ShowPetriDishResult()
    {
        if (GameStateManager_L.Instance == null || string.IsNullOrEmpty(GameStateManager_L.Instance.IncubatedPetriDishID))
        {
            Debug.LogError("오류: GameStateManager에 저장된 페트리 접시 ID가 없습니다!");
            return;
        }

        // 1. 씬에 있는 모든 ExperimentItem_L 컴포넌트를 찾습니다.
        ExperimentItem_L[] allItems = FindObjectsOfType<ExperimentItem_L>();
        GameObject foundPetriDish = null;

        // 2. 저장된 ID와 일치하는 아이템을 찾습니다.
        foreach (var item in allItems)
        {
            if (item.uniqueId == GameStateManager_L.Instance.IncubatedPetriDishID)
            {
                foundPetriDish = item.gameObject;
                break;
            }
        }

        // 3. 페트리 접시를 찾았다면, 결과 표시를 요청합니다.
        if (foundPetriDish != null)
        {
            PetriDishController_G petriDishController = foundPetriDish.GetComponent<PetriDishController_G>();
            if (petriDishController != null)
            {
                petriDishController.ShowResult();
                Debug.Log($"성공: ID({GameStateManager_L.Instance.IncubatedPetriDishID})로 찾은 페트리 접시의 결과 표시를 요청했습니다.");
            }
            else
            {
                Debug.LogError($"오류: 찾은 페트리 접시 '{foundPetriDish.name}'에서 PetriDishController_G를 찾지 못했습니다.");
            }
        }
        else
        {
            Debug.LogError($"오류: 저장된 ID({GameStateManager_L.Instance.IncubatedPetriDishID})와 일치하는 페트리 접시를 씬에서 찾지 못했습니다.");
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
//        Debug.Log($"장비 '{controller.name}' 상태 변경: {(isLocked ? "잠김" : "활성화")}");
    }

    public void OnGelElectrophoresisProcessStarted()
    {
        //Debug.Log(">> 매니저: GelElectrophoresis 작동 시작 감지. GelDoc에 분석 시작을 명령합니다.");
        if (gelDocScreenController != null && gelElectrophoresisController != null)
        {
            GameObject processingItem = gelElectrophoresisController.GetCurrentlyProcessingItem();
            if (processingItem != null)
            {
                ExperimentItem_L itemInfo = processingItem.GetComponent<ExperimentItem_L>();
                if (itemInfo != null)
                {
                    // GelDoc의 잠금을 해제하고, 활성화한 뒤, 분석을 시작시킵니다.
                    //gelDocScreenController.gameObject.SetActive(true); // 혹시 모르니 활성화
                    gelDocScreenController.SetLockState(false); // << 잠금 해제 (핵심 수정!)
                    gelDocScreenController.StartAnalysis(itemInfo.experimentGroup); // 분석 시작
                }
            }
        }
    }

    public void OnGelDocViewingStopped()
    {
        //Debug.Log(">> 매니저: GelDoc 보기 중단 감지. GelElectrophoresis에 아이템 배출을 명령합니다.");
        if (gelElectrophoresisController != null)
        {
            gelElectrophoresisController.MakeItemAvailable();
        }
    }

    // Air Incubator의 OnProcessCompleted 이벤트가 호출할 함수
    public void OnAirIncubatorFinished()
    {
        Debug.Log(">> 이벤트 수신: Air Incubator 완료. 페트리 접시 결과 표시를 시도합니다.");

        if (airIncubatorController == null)
        {
            Debug.LogError("오류: airIncubatorController 변수가 인스펙터에 할당되지 않았습니다!");
            return;
        }

        // 1. Air Incubator에게 방금 완료한 아이템을 직접 물어봅니다.
        GameObject finishedPetriDish = airIncubatorController.GetCompletedItem();
        if (finishedPetriDish == null)
        {
            Debug.LogError("오류: Air Incubator로부터 완료된 아이템 정보를 받아오지 못했습니다!");
            return;
        }

        // 2. 받아온 아이템에서 PetriDishController_G 스크립트를 찾아옵니다.
        PetriDishController_G petriDishController = finishedPetriDish.GetComponent<PetriDishController_G>();
        if (petriDishController != null)
        {
            // 3. 스크립트를 찾았다면, ShowResult() 함수를 호출합니다.
            petriDishController.ShowResult();
        }
        else
        {
            Debug.LogError($"오류: '{finishedPetriDish.name}' 오브젝트에서 PetriDishController_G 스크립트를 찾을 수 없습니다!");
        }
    }
}