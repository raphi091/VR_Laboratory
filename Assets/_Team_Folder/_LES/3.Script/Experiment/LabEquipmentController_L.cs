using System.Collections;
using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용


// 개별 실험 기기의 작동과 상태를 관리하는 범용 컨트롤러.
// 작동 완료 시 UnityEvent를 통해 외부(각 실험 매니저)에 알립니다.
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

    //이벤트
    [Header("이벤트")]
    [Tooltip("이 기기의 프로세스가 완료되었을 때 호출될 이벤트")]
    public UnityEvent OnProcessCompleted;

    private GameObject currentItem;

    //기기 작동 시작
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
        // Shaking Incubator 로직은 코루틴으로 이동했으므로 여기서 제외
        if (type == EquipmentType.GelDoc)
        {
            Renderer screenRenderer = transform.Find("Screen").GetComponent<Renderer>();
            screenRenderer.material.mainTexture = ResultManager_L.Instance.GetRandomPcrResult();
        }
    }

    private IEnumerator AnimateLiquidChange()
    {
        float elapsedTime = 0f;

        // currentItem(플라스크) 안의 액체 오브젝트 렌더러를 찾음
        Renderer liquidRenderer = currentItem.transform.Find("Liquid").GetComponent<Renderer>();
        if (liquidRenderer == null)
        {
            Debug.LogError("플라스크 모델 안에 'Liquid'라는 이름의 자식 오브젝트가 없습니다!");
            ProcessComplete(); // 코루틴을 즉시 종료하고 완료 처리
            yield break;
        }

        // ResultManager에서 시작 색상과 목표 색상을 가져옴
        Color startColor = ResultManager_L.Instance.flaskClearLiquidMaterial.color;
        Color endColor = ResultManager_L.Instance.flaskCloudyLiquidMaterial.color;

        // 원본 머티리얼을 복제하여 사용 (다른 플라스크에 영향 주지 않기 위함)
        Material newMaterialInstance = new Material(ResultManager_L.Instance.flaskClearLiquidMaterial);
        liquidRenderer.material = newMaterialInstance;

        // processingTime 동안 반복
        while (elapsedTime < processingTime)
        {
            // 경과 시간을 0과 1 사이의 값으로 정규화
            float t = elapsedTime / processingTime;

            // Color.Lerp를 사용하여 시작 색상과 목표 색상 사이의 중간 색상을 계산
            newMaterialInstance.color = Color.Lerp(startColor, endColor, t);

            // 경과 시간 업데이트 후 다음 프레임까지 대기
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 루프가 끝나면 최종 색상으로 확실하게 변경
        newMaterialInstance.color = endColor;

        // 모든 애니메이션이 끝난 후 완료 처리
        ProcessComplete();
    }
}