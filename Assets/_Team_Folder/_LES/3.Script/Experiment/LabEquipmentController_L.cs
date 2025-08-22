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

    [Tooltip("완료된 아이템이 놓일 위치")]
    public Transform completedItemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;

    [Header("UI 요소")]
    [Tooltip("버튼들이 있는 Panel 게임 오브젝트를 연결해주세요.")]
    public GameObject yesNoButtonPanelObject;
    private CanvasGroup yesNoButtonPanelCanvasGroup;

    [Header("Optional Visual Components")]
    public Renderer screenRenderer;

    [Header("사운드 설정")]
    [Tooltip("기기 작동 시작 시 재생할 사운드")]
    public AudioClip startSound;
    [Tooltip("기기 작동 중에 반복 재생할 사운드")]
    public AudioClip processingLoopSound;
    [Tooltip("기기 작동 완료 시 재생할 사운드")]
    public AudioClip completeSound;

    [Header("이벤트")]
    public UnityEvent OnProcessCompleted;

    // 내부 상태 변수
    private AudioSource audioSource;
    private GameObject itemInRange;
    private GameObject itemToProcess; // 처리할 아이템을 저장하는 변수 추가
    private XRGrabInteractable itemInteractable;
    private bool uiVisible = false;
    private bool handInRange = false;
    private bool readyToProcess = false; // 처리 준비 상태 추가
    private GameObject completedItem;

    void Awake()
    {
        // 게임이 시작될 때 UI가 확실하게 숨겨진 상태로 시작하도록 보장합니다.
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = 0;
            interactionCanvasGroup.interactable = false;
            interactionCanvasGroup.blocksRaycasts = false;
        }

        if (yesNoButtonPanelObject != null)
        {
            yesNoButtonPanelCanvasGroup = yesNoButtonPanelObject.GetComponent<CanvasGroup>();
            if (yesNoButtonPanelCanvasGroup == null)
            {
                Debug.LogError($"{gameObject.name}: yesNoButtonPanelObject에 CanvasGroup 컴포넌트가 없습니다! 추가해주세요.");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: yesNoButtonPanelObject가 Inspector에 연결되지 않았습니다!");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning($"{gameObject.name}에 AudioSource 컴포넌트가 없어 사운드를 재생할 수 없습니다. 컴포넌트를 추가합니다.");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // 자동 재생 방지
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
            if (handInRange) UpdateInteractionUI();
        }
    }

    private void UpdateInteractionUI()
    {
        // 기기 작업이 완료된 상태일 때의 로직
        if (currentState == MachineState.Complete)
        {
            // 1. 완료 상태 텍스트("완료! 아이템을 회수하세요.")를 설정합니다.
            UpdateStatusText(false);

            // 2. 텍스트 UI를 화면에 '표시'합니다. (false -> true로 변경)
            SetUIVisible(true, false);

            // 3. 손이 범위 안에 들어왔을 때만 아이템 잠금을 해제합니다.
            if (handInRange && completedItem != null)
            {
                Debug.Log("손이 감지되어 완료된 아이템을 회수 가능하도록 잠금 해제합니다.");
                UnlockItem(completedItem); // 아이템 잠금 해제
                completedItem = null;      // 잠금 해제는 한 번만 실행되도록 초기화
            }
            return; // 완료 상태에서는 다른 UI 업데이트 로직을 실행하지 않도록 여기서 종료합니다.
        }

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

        if (hasItemInHand) // 조건을 단순화: 아이템을 손에 쥐고 있기만 하면 됨
        {
            itemToProcess = itemInRange;
            readyToProcess = true;
            UpdateStatusText(true);   // "이 기기를 사용하시겠습니까?"
            SetUIVisible(true, true); // 텍스트와 '버튼' 모두 표시
        }
        else if (itemInRange != null)
        {
            // 아이템은 범위에 있지만 손에 쥐고 있지 않을 때
            readyToProcess = false;
            itemToProcess = null;
            statusText.text = "아이템을 잡고 가져오세요.";
            SetUIVisible(true, false); // 텍스트만 표시하고 '버튼'은 숨김
        }
        else
        {
            // 손만 범위에 있을 때
            readyToProcess = false;
            itemToProcess = null;
            UpdateStatusText(false);
            SetUIVisible(true, false); // 텍스트만 표시하고 '버튼'은 숨김
        }
    }

    public void StartProcessing()
    {
        // 수정된 조건: readyToProcess와 itemToProcess를 체크
        if (!readyToProcess || itemToProcess == null || currentState != MachineState.Idle)
        {
            Debug.LogWarning($"{gameObject.name}: 처리 시작 실패 - 조건 미충족");
            Debug.LogWarning($"  실패 이유: readyToProcess={readyToProcess}, itemToProcess={(itemToProcess != null ? "있음" : "NULL")}, currentState={currentState}");
            return;
        }

        Debug.Log($"{gameObject.name}: 처리 시작 - 아이템: {itemToProcess.name}");
        currentState = MachineState.Processing;

        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();

            // 1. 시작 사운드가 있다면 재생
            if (startSound != null)
            {
                audioSource.PlayOneShot(startSound);
            }

            // 2. 루프 사운드가 있다면, 이어서 재생 (약간의 딜레이를 줘서 시작 사운드와 겹치지 않게 함)
            if (processingLoopSound != null)
            {
                // 이전 코루틴이 있다면 중지
                StopAllCoroutines();
                StartCoroutine(PlayLoopSoundAfterDelay(startSound != null ? startSound.length : 0f));
            }
        }

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

        if (audioSource != null)
        {
            // 1. 시끄러운 루프 사운드는 정지
            audioSource.Stop();
            audioSource.loop = false;

            // 2. 완료 알림 사운드 (한 번 재생)
            if (completeSound != null)
            {
                audioSource.PlayOneShot(completeSound);
            }
        }

        completedItem = targetItem;

        if (idleModelObject != null) idleModelObject.SetActive(true);
        if (processingModelObject != null) processingModelObject.SetActive(false);

        HandleVisualResult(targetItem);
        UnlockItem(targetItem);

        OnProcessCompleted.Invoke();

        if (completedItemPlacementPoint != null)
        {
            targetItem.transform.position = completedItemPlacementPoint.position;
            targetItem.transform.rotation = completedItemPlacementPoint.rotation;
            Debug.Log($"아이템을 완료 지점 '{completedItemPlacementPoint.name}'으로 이동시켰습니다.");
        }

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

        if (yesNoButtonPanelCanvasGroup != null)
        {
            // visible과 showButtons가 모두 true일 때만 버튼을 표시합니다.
            bool shouldShowButtons = visible && showButtons;
            yesNoButtonPanelCanvasGroup.alpha = shouldShowButtons ? 1f : 0f;
            yesNoButtonPanelCanvasGroup.interactable = shouldShowButtons;
            yesNoButtonPanelCanvasGroup.blocksRaycasts = shouldShowButtons;
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
            Debug.Log($"{gameObject.name}: 기기 상태가 'Complete'로 유지됩니다.");
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

    private IEnumerator PlayLoopSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 딜레이 후에도 여전히 Processing 상태일 때만 루프 사운드를 재생합니다.
        if (audioSource != null && currentState == MachineState.Processing)
        {
            audioSource.clip = processingLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
}