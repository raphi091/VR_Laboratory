using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용

/// <summary>
/// 개별 실험 기기의 작동과 상태를 관리하는 범용 컨트롤러.
/// 작동 완료 시 UnityEvent를 통해 외부(각 실험 매니저)에 알립니다.
/// </summary>
public class LabEquipmentController_L : MonoBehaviour
{
    public enum EquipmentType
    {
        Thermocycler,
        GelElectrophoresis,
        GelDoc,
        Autoclave,
        ShakingIncubator,
        AirIncubator,
        CleanBench
    }
    
    [Header("기기 식별")]
    public EquipmentType type;
    
    [Header("상태 및 설정")]
    [SerializeField] private float processingTime = 5.0f;
    public Transform itemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;
    public string requiredItemTag = "ExperimentSample";

    // --- 이벤트 ---
    [Header("이벤트")]
    [Tooltip("이 기기의 프로세스가 완료되었을 때 호출될 이벤트")]
    public UnityEvent OnProcessCompleted;
    
    private GameObject currentItem;

    // 기기 작동을 시작하는 함수 (UI 버튼 등에서 호출)
    public void StartProcessing()
    {
        // ... (아이템을 기기 안에 넣고 잠그는 로직) ...
        // 예: currentItem = ...; currentItem.transform.SetParent(itemPlacementPoint);

        // 타이머 시작 또는 상태 변경
        Invoke(nameof(ProcessComplete), processingTime);
    }

    // 프로세스가 완료되었을 때 호출되는 함수
    private void ProcessComplete()
    {
        Debug.Log($"{type}의 작업이 완료되었습니다.");

        // 아이템의 시각적 변화 처리
        HandleVisualResult();

        // 등록된 모든 리스너(매니저의 함수)들에게 작업 완료를 알림
        OnProcessCompleted.Invoke();
        
        // ... (아이템을 다시 꺼낼 수 있게 잠금 해제하는 로직) ...
    }
    
    // 기기 종류에 따라 시각적 결과물을 처리
    private void HandleVisualResult()
    {
        // Shaking Incubator는 액체 색을 바꿔야 함
        if (type == EquipmentType.ShakingIncubator)
        {
            if (currentItem != null && ResultManager_L.Instance != null)
            {
                // currentItem은 플라스크, 그 안의 액체 오브젝트의 렌더러를 찾아야 함
                Renderer liquidRenderer = currentItem.transform.Find("Liquid").GetComponent<Renderer>();
                liquidRenderer.material = ResultManager_L.Instance.flaskCloudyLiquidMaterial;
            }
        }
        // GelDoc은 결과 이미지를 띄워야 함
        else if (type == EquipmentType.GelDoc)
        {
            // GelDoc 화면 오브젝트의 렌더러를 찾아 텍스처 적용
            Renderer screenRenderer = transform.Find("Screen").GetComponent<Renderer>();
            screenRenderer.material.mainTexture = ResultManager_L.Instance.GetRandomPcrResult();
        }
    }
}