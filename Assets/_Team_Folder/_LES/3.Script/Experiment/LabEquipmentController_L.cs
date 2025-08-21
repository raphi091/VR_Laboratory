using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class LabEquipmentController_L : MonoBehaviour
{
    // 기기의 현재 상태를 관리하기 위한 Enum
    private enum MachineState { Idle, Processing, Complete }
    private MachineState currentState = MachineState.Idle;

    public enum EquipmentType { Thermocycler, GelElectrophoresis, GelDoc, Autoclave, ShakingIncubator, AirIncubator, CleanBench }

    [Header("기기 식별")]
    public EquipmentType type;

    [Header("모델 오브젝트 설정")]
    [Tooltip("기기가 대기 상태일 때 표시될 모델")]
    public GameObject idleModelObject;

    [Tooltip("기기가 작동 중일 때 표시될 모델")]
    public GameObject processingModelObject;

    [Header("상태 및 설정")]
    [SerializeField] private float processingTime = 5.0f;
    public Transform itemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;

    [Header("Optional Visual Components")]
    public Renderer screenRenderer;

    [Header("이벤트")]
    public UnityEvent OnProcessCompleted;

    // 내부 상태 변수
    private GameObject itemInRange;
    private GameObject itemToProcess; // 처리할 아이템을 저장하는 변수 추가
    private XRGrabInteractable itemInteractable;
    private bool uiVisible = false;
    private bool handInRange = false;
    private bool readyToProcess = false; // 처리 준비 상태 추가

    void Awake()
    {
        // 게임이 시작될 때 UI가 확실하게 숨겨진 상태로 시작하도록 보장합니다.
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
            interactionCanvasGroup.blocksRaycasts = false;
        }
    }

    void Start()
    {
        // Start에서도 한 번 더 확인 (Awake 이후 Inspector 설정이 적용될 수 있음)
        SetUIVisible(false, false);

        // 디버그 로그
        Debug.Log($"{gameObject.name}: UI 초기화 완료 - Canvas Alpha: {interactionCanvasGroup?.alpha}");

        if (idleModelObject != null) idleModelObject.SetActive(true);
        if (processingModelObject != null) processingModelObject.SetActive(false);
    }

    void Update()
    {
        if (handInRange)
        {
            UpdateInteractionUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isHand = other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand");
        ExperimentItem_L experimentItem = other.GetComponent<ExperimentItem_L>();

        if (isHand)
        {
            handInRange = true;
            Debug.Log($"{gameObject.name}: 손 '{other.tag}' 감지됨");
            UpdateInteractionUI();
        }
        else if (experimentItem != null)
        {
            itemInRange = other.gameObject;
            itemInteractable = itemInRange.GetComponent<XRGrabInteractable>();
            Debug.Log($"{gameObject.name}: 아이템 '{other.name}' (타입: {experimentItem.itemType}) 감지됨");
            if (handInRange) UpdateInteractionUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bool isHand = other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand");
        bool isItem = (other.gameObject == itemInRange);

        if (isHand)
        {
            handInRange = false;
            readyToProcess = false; // 손이 범위를 벗어나면 준비 상태 해제
            itemToProcess = null;
            Debug.Log($"{gameObject.name}: 손 '{other.tag}' 범위 벗어남");
            SetUIVisible(false, false); 
        }
        else if (isItem)
        {
            Debug.Log($"{gameObject.name}: 아이템 '{other.name}' 범위 벗어남");
            itemInRange = null;
            itemInteractable = null;
            readyToProcess = false;
            itemToProcess = null;
            if(handInRange) UpdateInteractionUI(); 
        }
    }

    private void UpdateInteractionUI()
    {
        // 처리 중일 때는 아무것도 표시하지 않음
        if (currentState == MachineState.Processing)
        {
            statusText.text = "처리 중...";
            SetUIVisible(true, false);
            return;
        }

        // isSelected 체크 대신 아이템이 손 근처에 있는지 체크하는 대체 방법
        bool hasItemInHand = false;
        
        if (itemInRange != null && itemInteractable != null)
        {
            // 방법 1: isSelected 체크 (원래 방법)
            hasItemInHand = itemInteractable.isSelected;
            
            // 방법 2: 만약 isSelected가 작동하지 않으면, 아이템의 부모가 손인지 체크
            if (!hasItemInHand && itemInRange.transform.parent != null)
            {
                Transform parent = itemInRange.transform.parent;
                hasItemInHand = parent.CompareTag("Right_Hand") || parent.CompareTag("Left_Hand");
            }
            
            // 방법 3: 아이템이 기기 근처에 있고 손도 근처에 있으면 true
            if (!hasItemInHand && handInRange && itemInRange != null)
            {
                // 단순히 손과 아이템이 모두 범위 내에 있으면 처리 가능으로 판단
                hasItemInHand = true;
            }
        }
        
        // 디버깅 로그
        Debug.Log($"UpdateUI - item: {itemInRange?.name}, interactable: {itemInteractable != null}, " +
                  $"isSelected: {itemInteractable?.isSelected}, hasItemInHand: {hasItemInHand}");

        if (hasItemInHand && itemInRange != null)  // itemInRange null 체크 추가
        {
            // 1순위: 아이템을 손에 들고 있을 때
            // 처리할 아이템 미리 저장
            itemToProcess = itemInRange;
            readyToProcess = true;
            
            UpdateStatusText(true); // "이 기기를 사용하시겠습니까?"
            SetUIVisible(true, true); // 버튼과 함께 UI 표시
            
            // 더 자세한 디버깅 정보
            Debug.Log($"{gameObject.name}: 처리 준비 완료");
            Debug.Log($"  - itemToProcess 할당됨: {(itemToProcess != null ? "YES" : "NO")}");
            Debug.Log($"  - itemToProcess 이름: {(itemToProcess != null ? itemToProcess.name : "NULL")}");
            Debug.Log($"  - readyToProcess: {readyToProcess}");
        }
        else if (itemInRange != null)
        {
            // 2순위: 아이템은 있지만 손에 들고 있지 않을 때
            readyToProcess = false;
            itemToProcess = null;
            statusText.text = "아이템을 잡고 가져오세요.";
            SetUIVisible(true, false);
        }
        else
        {
            // 3순위: 손만 있을 때 (Idle 또는 Complete 상태)
            readyToProcess = false;
            itemToProcess = null;
            UpdateStatusText(false); // 상태에 맞는 텍스트("아이템을 올려주세요" 등)
            SetUIVisible(true, false);
        }
    }

    public void StartProcessing()
    {
        Debug.Log(1);

        // 수정된 조건: readyToProcess와 itemToProcess를 체크
        if (!readyToProcess || itemToProcess == null || currentState != MachineState.Idle)
        {
            Debug.LogWarning($"{gameObject.name}: 처리 시작 실패 - 조건 미충족");
            Debug.LogWarning($"  실패 이유: readyToProcess={readyToProcess}, itemToProcess={(itemToProcess != null ? "있음" : "NULL")}, currentState={currentState}");
            return;
        }

        Debug.Log(2);

        Debug.Log($"{gameObject.name}: 처리 시작 - 아이템: {itemToProcess.name}");
        currentState = MachineState.Processing;

        if (idleModelObject != null) idleModelObject.SetActive(false);
        if (processingModelObject != null) processingModelObject.SetActive(true);

        SetUIVisible(false, false);

        // 저장해둔 아이템으로 처리 진행
        GameObject processingItem = itemToProcess;
        XRGrabInteractable processingInteractable = processingItem.GetComponent<XRGrabInteractable>();

        ForceDropItem(processingItem, processingInteractable);
        LockItemInPlace(processingItem);

        // 처리 완료 후 상태 초기화
        readyToProcess = false;
        itemToProcess = null;

        if (type == EquipmentType.ShakingIncubator)
        {
            StartCoroutine(AnimateLiquidChange(processingItem));
        }
        else
        {
            StartCoroutine(ProcessTimer(processingItem));
        }
    }

    // No 버튼 클릭 시 호출될 메서드
    public void CancelProcessing()
    {
        Debug.Log($"{gameObject.name}: 처리 취소");
        readyToProcess = false;
        itemToProcess = null;
        SetUIVisible(false, false);
    }

    private IEnumerator ProcessTimer(GameObject targetItem)
    {
        float elapsedTime = 0;
        while (elapsedTime < processingTime)
        {
            if (statusText != null)
            {
                statusText.text = $"처리 중... {Mathf.RoundToInt(processingTime - elapsedTime)}초";
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        ProcessComplete(targetItem);
    }

    private void ProcessComplete(GameObject targetItem)
    {
        Debug.Log($"{gameObject.name}: 처리 완료");
        currentState = MachineState.Complete;

        if (idleModelObject != null) idleModelObject.SetActive(true);
        if (processingModelObject != null) processingModelObject.SetActive(false);

        HandleVisualResult(targetItem);
        UnlockItem(targetItem);

        OnProcessCompleted.Invoke();

        // 완료 상태 텍스트 업데이트
        if (statusText != null)
        {
            statusText.text = "완료! 아이템을 회수하세요.";
        }
    }

    private void SetUIVisible(bool visible, bool showButtons)
    {
        // Canvas Group 설정
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = visible ? 1 : 0;
            interactionCanvasGroup.interactable = visible;
            interactionCanvasGroup.blocksRaycasts = visible;
        }

        uiVisible = visible;
        Debug.Log($"{gameObject.name}: UI 상태 변경 - Visible: {visible}, Buttons: {showButtons}");
    }

    private void UpdateStatusText(bool isItemInteraction)
    {
        if (statusText == null) return;

        if (isItemInteraction)
        {
            statusText.text = "이 기기를 사용하시겠습니까?";
            return;
        }

        switch (currentState)
        {
            case MachineState.Idle:
                statusText.text = "아이템을 올려주세요.";
                break;
            case MachineState.Processing:
                statusText.text = "처리 중...";
                break;
            case MachineState.Complete:
                statusText.text = "완료. 아이템을 회수하세요.";
                break;
        }
    }

    private void ForceDropItem(GameObject item, XRGrabInteractable interactable)
    {
        if (interactable != null && interactable.isSelected)
        {
            IXRSelectInteractor interactor = interactable.firstInteractorSelecting;
            if (interactor != null)
            {
                XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();
                if (interactionManager != null)
                {
                    interactionManager.SelectExit(interactor, interactable);
                }
            }
        }
    }

    private void LockItemInPlace(GameObject item)
    {
        var interactable = item.GetComponent<Ch_VelocityInteractable>();
        if (interactable != null) interactable.enabled = false;

        var itemCollider = item.GetComponent<Collider>();
        var itemRigidbody = item.GetComponent<Rigidbody>();

        if (itemRigidbody != null) itemRigidbody.isKinematic = true;
        if (itemCollider != null) itemCollider.enabled = false;

        item.transform.SetParent(itemPlacementPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    private void UnlockItem(GameObject item)
    {
        if (item == null) return;

        var interactable = item.GetComponent<Ch_VelocityInteractable>();
        if (interactable != null) interactable.enabled = true;

        var itemCollider = item.GetComponent<Collider>();
        var itemRigidbody = item.GetComponent<Rigidbody>();

        item.transform.SetParent(null);
        if (itemRigidbody != null) itemRigidbody.isKinematic = false;
        if (itemCollider != null) itemCollider.enabled = true;

        // 아이템 회수 후 상태 리셋
        if (currentState == MachineState.Complete)
        {
            currentState = MachineState.Idle;
            Debug.Log($"{gameObject.name}: 기기 상태를 Idle로 리셋");
        }
    }

    private void HandleVisualResult(GameObject targetItem)
    {
        if (type == EquipmentType.GelDoc)
        {
            if (screenRenderer != null && ResultManager_L.Instance != null)
            {
                screenRenderer.material.mainTexture = ResultManager_L.Instance.GetRandomPcrResult();
            }
        }
    }

    private IEnumerator AnimateLiquidChange(GameObject targetItem)
    {
        var liquidRenderer = targetItem.GetComponentInChildren<Renderer>();
        if (liquidRenderer == null)
        {
            ProcessComplete(targetItem);
            yield break;
        }

        float elapsedTime = 0f;
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
        newMaterialInstance.color = endColor;

        ProcessComplete(targetItem);
    }

    // Inspector에서 설정 확인용
    void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (interactionCanvasGroup == null)
        {
            Debug.LogWarning($"{gameObject.name}: interactionCanvasGroup이 할당되지 않았습니다!");
        }
    }
}