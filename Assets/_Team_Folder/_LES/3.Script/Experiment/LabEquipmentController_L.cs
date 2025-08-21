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

    [Header("상태 및 설정")]
    [SerializeField] private float processingTime = 5.0f;
    public Transform itemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;
    public string requiredItemTag = "ExperimentSample";

    [Header("UI 요소")]
    public GameObject yesNoButtonPanel;

    [Header("Optional Visual Components")]
    public Renderer screenRenderer;

    [Header("이벤트")]
    public UnityEvent OnProcessCompleted;

    // 내부 상태 변수
    private GameObject itemInRange;
    private XRGrabInteractable itemInteractable;
    private bool uiVisible = false;
    private bool handInRange = false;

    void Awake()
    {
        // 게임이 시작될 때 UI가 확실하게 숨겨진 상태로 시작하도록 보장합니다.
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
            interactionCanvasGroup.blocksRaycasts = false;
        }
        
        // yesNoButtonPanel도 확실히 비활성화
        if (yesNoButtonPanel != null)
        {
            yesNoButtonPanel.SetActive(false);
        }
    }

    void Start()
    {
        // Start에서도 한 번 더 확인 (Awake 이후 Inspector 설정이 적용될 수 있음)
        SetUIVisible(false, false);
        
        // 디버그 로그
        Debug.Log($"{gameObject.name}: UI 초기화 완료 - Canvas Alpha: {interactionCanvasGroup?.alpha}, Panel Active: {yesNoButtonPanel?.activeSelf}");
    }

    void Update()
    {        
        // 처리 중일 때는 아무 UI 상호작용도 하지 않음
        if (currentState == MachineState.Processing)
        {
            if (uiVisible) SetUIVisible(false, false);
            return;
        }

        // [수정된 로직] 각 상황을 독립적으로 체크
        bool shouldShowUI = false;
        bool shouldShowButtons = false;
        
        // 아이템을 잡고 있고 범위 안에 있는지 체크
        bool hasItemInHand = (itemInRange != null && itemInteractable != null && itemInteractable.isSelected);
        
        // UI 표시 조건 결정
        if (hasItemInHand)
        {
            // 아이템을 잡고 있을 때 - 버튼과 함께 UI 표시
            shouldShowUI = true;
            shouldShowButtons = true;
            UpdateStatusText(true); // "사용하시겠습니까?" 텍스트
        }
        else if (handInRange && currentState != MachineState.Idle)
        {
            // 손만 있고 기기가 Idle 상태가 아닐 때만 상태 표시
            shouldShowUI = true;
            shouldShowButtons = false;
            UpdateStatusText(false); // 기기 상태 텍스트
        }
        else if (handInRange && itemInRange != null && !itemInteractable.isSelected)
        {
            // 손과 아이템이 모두 범위에 있지만 아이템을 잡고 있지 않을 때
            shouldShowUI = true;
            shouldShowButtons = false;
            statusText.text = "아이템을 잡고 가져오세요.";
        }
        
        // UI 상태 업데이트
        if (shouldShowUI && !uiVisible)
        {
            SetUIVisible(true, shouldShowButtons);
        }
        else if (!shouldShowUI && uiVisible)
        {
            SetUIVisible(false, false);
        }
        else if (uiVisible && yesNoButtonPanel != null && yesNoButtonPanel.activeSelf != shouldShowButtons)
        {
            // UI는 보이지만 버튼 상태만 변경이 필요한 경우
            yesNoButtonPanel.SetActive(shouldShowButtons);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아이템 체크
        if (other.CompareTag(requiredItemTag))
        {
            itemInRange = other.gameObject;
            itemInteractable = itemInRange.GetComponent<XRGrabInteractable>();
            Debug.Log($"{gameObject.name}: 아이템 '{other.name}' 감지됨");
        }
        // 손 체크
        else if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            handInRange = true;
            Debug.Log($"{gameObject.name}: 손 '{other.tag}' 감지됨");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 아이템 체크
        if (other.gameObject == itemInRange)
        {
            itemInRange = null;
            itemInteractable = null;
            Debug.Log($"{gameObject.name}: 아이템 '{other.name}' 범위 벗어남");
        }
        // 손 체크
        else if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            handInRange = false;
            Debug.Log($"{gameObject.name}: 손 '{other.tag}' 범위 벗어남");
        }
    }

    public void StartProcessing()
    {
        // 처리 시작 조건 체크
        if (itemInRange == null || itemInteractable == null || !itemInteractable.isSelected || currentState != MachineState.Idle)
        {
            Debug.LogWarning($"{gameObject.name}: 처리 시작 실패 - 조건 미충족");
            return;
        }

        Debug.Log($"{gameObject.name}: 처리 시작");
        currentState = MachineState.Processing;
        SetUIVisible(false, false);

        GameObject itemToProcess = itemInRange;
        
        ForceDropItem(itemToProcess, itemInteractable);
        LockItemInPlace(itemToProcess);

        if (type == EquipmentType.ShakingIncubator)
        {
            StartCoroutine(AnimateLiquidChange(itemToProcess));
        }
        else
        {
            StartCoroutine(ProcessTimer(itemToProcess));
        }
    }

    // No 버튼 클릭 시 호출될 메서드 추가
    public void CancelProcessing()
    {
        Debug.Log($"{gameObject.name}: 처리 취소");
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
        
        // Yes/No 버튼 패널 설정
        if (yesNoButtonPanel != null)
        {
            yesNoButtonPanel.SetActive(visible && showButtons); // visible이 true일 때만 showButtons 체크
        }
        
        uiVisible = visible;
        
        // 디버그 로그
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
        
        if (yesNoButtonPanel == null)
        {
            Debug.LogWarning($"{gameObject.name}: yesNoButtonPanel이 할당되지 않았습니다!");
        }
    }
}