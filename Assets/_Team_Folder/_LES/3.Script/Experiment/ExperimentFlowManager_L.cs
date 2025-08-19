using UnityEngine;

// 기기 사용 순서에 따라 다음 기기를 활성화하는 방식으로
// 실험의 전체 흐름을 간단하게 관리합니다.
public class ExperimentFlowManager_L : MonoBehaviour
{
    [Header("PCR Experiment Equipment")]
    public GameObject thermocycler;
    public GameObject gelElectrophoresis;
    public GameObject gelDoc;

    [Header("Culturing Experiment Equipment")]
    public GameObject autoclave;
    public GameObject shakingIncubator;
    public GameObject airIncubator;
    //Clean Bench 등 다른 기기들도 필요에 따라 추가

    void Start()
    {
        //PCR 초기 상태
        // 처음에는 Thermocycler만 사용 가능하도록 설정
        SetEquipmentActive(thermocycler, true);
        SetEquipmentActive(gelElectrophoresis, false);
        SetEquipmentActive(gelDoc, false);

        //미생물 배양 초기 상태
        // 처음에는 Autoclave만 사용 가능하도록 설정
        SetEquipmentActive(autoclave, true);
        SetEquipmentActive(shakingIncubator, false);
        SetEquipmentActive(airIncubator, false);
    }

    // 각 기기의 OnProcessCompleted 이벤트에 연결할 함수들
    public void OnThermocyclerFinished()
    {
        Debug.Log("Thermocycler 완료, Gel Electrophoresis 활성화.");
        SetEquipmentActive(gelElectrophoresis, true);
    }

    public void OnGelElectrophoresisFinished()
    {
        Debug.Log("Gel Electrophoresis 완료, Gel Doc 활성화.");
        SetEquipmentActive(gelDoc, true);
    }

    public void OnAutoclaveFinished()
    {
        Debug.Log("Autoclave 완료, Shaking Incubator 활성화.");
        SetEquipmentActive(shakingIncubator, true);
    }

    public void OnShakingIncubatorFinished()
    {
        Debug.Log("Shaking Incubator 완료, Air Incubator 활성화.");
        // 실제로는 Clean Bench에서 다른 작업을 거친 후 Air Incubator로 가게 됩니다.
        // 이 부분의 흐름은 필요에 따라 커스터마이징이 가능합니다.
        SetEquipmentActive(airIncubator, true);
    }

    // 기기 오브젝트와 그 상호작용 컴포넌트들을 활성화/비활성화합니다.
    private void SetEquipmentActive(GameObject equipment, bool isActive)
    {
        if (equipment == null) return;

        // 기기 자체의 콜라이더나 스크립트를 제어할 수 있습니다.
        // 예를 들어 LabEquipmentController_L 스크립트를 켜고 끌 수 있습니다.
        var controller = equipment.GetComponent<LabEquipmentController_L>();
        if (controller != null)
        {
            controller.enabled = isActive;
        }

        // 또는 기기와 상호작용하는 UI 캔버스 그룹을 직접 제어할 수도 있습니다.
        var canvasGroup = equipment.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = isActive;
            // 비활성화 시 눈에 잘 띄도록 반투명하게 만들 수도 있습니다.
            canvasGroup.alpha = isActive ? 1.0f : 0.3f;
        }
    }
}