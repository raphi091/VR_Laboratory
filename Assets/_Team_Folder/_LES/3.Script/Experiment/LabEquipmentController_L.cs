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

    [Header("Optional Visual Components")] //시각적 처리를 위한 컴포넌트
    public Renderer screenRenderer; //GelDoc의 스크린
    public Renderer liquidRenderer; //Shaking Incubator의 액체

    //이벤트
    [Header("이벤트")]
    [Tooltip("이 기기의 프로세스가 완료되었을 때 호출될 이벤트")]
    public UnityEvent OnProcessCompleted;

    private GameObject currentItem;
    private Collider itemCollider;
    private Rigidbody itemRigidbody;

    #region Unity Lifecycle & Triggers
    void Start()
    {
        // UI가 있다면 초기에 숨김
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아직 기기가 작동 중이 아니고, 올바른 아이템이 들어왔을 때
        if (currentItem == null && other.CompareTag(requiredItemTag))
        {
            currentItem = other.gameObject;
            itemCollider = currentItem.GetComponent<Collider>();
            itemRigidbody = currentItem.GetComponent<Rigidbody>();
            // itemInteractable = currentItem.GetComponent<XRGrabInteractable>(); // VR용

            // UI 표시
            if (interactionCanvasGroup != null)
            {
                interactionCanvasGroup.alpha = 1;
                interactionCanvasGroup.interactable = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // UI에 선택지가 떠 있는 상태에서 아이템을 멀리 떨어뜨렸을 때
        if (other.gameObject == currentItem)
        {
            ResetState();
        }
    }
    #endregion

    #region Processing Logic
    //기기 작동 시작
    public void StartProcessing()
    {
        if (currentItem == null) return;
        
        // UI 숨기기
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
        }

        LockItemInPlace();

        if (type == EquipmentType.ShakingIncubator)
        {
            StartCoroutine(AnimateLiquidChange());
        }
        else
        {
            Invoke(nameof(ProcessComplete), processingTime);
        }
    }

    // 프로세스가 완료되었을 때 호출되는 함수
    private void ProcessComplete()
    {
        Debug.Log($"{type}의 작업이 완료되었습니다.");

        if (type != EquipmentType.ShakingIncubator)
        {
            HandleVisualResult();
        }

        UnlockItem();

        OnProcessCompleted.Invoke();
    }
    #endregion

    #region Helper Methods
    private void LockItemInPlace()
    {
        // 1. 물리적 충돌 및 이동 비활성화
        if (itemRigidbody != null) itemRigidbody.isKinematic = true;
        if (itemCollider != null) itemCollider.enabled = false;
        // if (itemInteractable != null) itemInteractable.enabled = false; // VR에서 잡는 기능 비활성화

        // 2. 기기 내부의 지정된 위치로 아이템 이동 및 종속
        currentItem.transform.SetParent(itemPlacementPoint);
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;
    }

    private void UnlockItem()
    {
        if (currentItem == null) return;

        // 1. 아이템을 다시 독립시킴
        currentItem.transform.SetParent(null);

        // 2. 물리 및 잡는 기능 다시 활성화
        if (itemRigidbody != null) itemRigidbody.isKinematic = false;
        if (itemCollider != null) itemCollider.enabled = true;
        // if (itemInteractable != null) itemInteractable.enabled = true; // VR에서 잡는 기능 활성화
    }

    // 초기 상태로 리셋
    private void ResetState()
    {
        currentItem = null;
        itemCollider = null;
        itemRigidbody = null;
        // itemInteractable = null;

        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
        }
    }

    // 코루틴 및 시각적 결과 처리 로직 (이전과 동일)
    private IEnumerator AnimateLiquidChange()
    {
        float elapsedTime = 0f;
        // Renderer liquidRenderer = currentItem.transform.Find("Liquid")?.GetComponent<Renderer>();
        if (liquidRenderer == null)
        {
            Debug.LogError("플라스크 모델 안에 'Liquid'라는 이름의 자식 오브젝트가 없습니다!");
            ProcessComplete();
            yield break;
        }

        Color startColor = ResultManager_L.Instance.flaskClearLiquidMaterial.color;
        Color endColor = ResultManager_L.Instance.flaskCloudyLiquidMaterial.color;
        Material newMaterialInstance = new Material(ResultManager_L.Instance.flaskClearLiquidMaterial);
        liquidRenderer.material = newMaterialInstance;

        while (elapsedTime < processingTime)
        {
            float t = elapsedTime / processingTime;
            newMaterialInstance.color = Color.Lerp(startColor, endColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 루프가 끝나면 최종 색상으로 확실하게 변경
        newMaterialInstance.color = endColor;

        // 모든 애니메이션이 끝난 후 완료 처리
        ProcessComplete();
    }

    private void HandleVisualResult()
    {
        if (type == EquipmentType.GelDoc)
        {
            if(screenRenderer != null)
                screenRenderer.material.mainTexture = ResultManager_L.Instance.GetRandomPcrResult();
        }
    }
    #endregion
}