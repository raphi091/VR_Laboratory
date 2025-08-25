using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class LabEquipmentController_L : MonoBehaviour
{
    // 기기의 현재 상태를 관리하기 위한 Enum
    private enum MachineState { Idle, Processing, Complete }
    private MachineState currentState = MachineState.Idle;

    public enum EquipmentType { Thermocycler, GelElectrophoresis, GelDoc, Autoclave, ShakingIncubator, AirIncubator }

    [Header("기기 식별")]
    public EquipmentType type;

    [Header("아이템 요구사항")]
    [Tooltip("체크하면, 아래에 지정된 특정 종류의 아이템만 인식합니다.")]
    public bool requireSpecificItemType = false;
    [Tooltip("이 기계가 요구하는 아이템의 종류")]
    public ItemType requiredItemType = ItemType.Generic;

    [Header("모델 오브젝트 설정")]
    [Tooltip("기기가 대기 상태일 때 표시될 모델")]
    public GameObject idleModelObject;

    [Tooltip("기기가 작동 중일 때 표시될 모델")]
    public GameObject processingModelObject;

    [Header("상태 및 설정")]
    [SerializeField] private float processingTime = 5.0f;
    [Tooltip("체크하면, 외부에서 멈출 때까지 무한히 작동합니다.")]
    public bool processInfinitely = false;
    public Transform itemPlacementPoint;

    [Header("Air Incubator 전용 설정")]
    [Tooltip("Air Incubator의 작동 조명")]
    public Light processingLight;
    [Tooltip("체크하면 아래에 설정된 시간 후 결과가 나오는 디버그 모드로 작동합니다.")]
    public bool debugMode = false;
    [Tooltip("디버그 모드일 때의 처리 시간")]
    public float debugProcessingTime = 10.0f;

    [Tooltip("완료된 아이템이 놓일 위치")]
    public Transform completedItemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;

    [Header("UI 요소")]
    [Tooltip("버튼들이 있는 Panel 게임 오브젝트를 연결해주세요.")]
    public GameObject yesNoButtonPanelObject;
    private CanvasGroup yesNoButtonPanelCanvasGroup;

    [Header("Optional Visual Components")]
    [Tooltip("결과 이미지를 표시할 UI RawImage 컴포넌트")]
    public RawImage resultRawImage;

    [Header("사운드 설정")]
    [Tooltip("기기 작동 시작 시 재생할 사운드")]
    public AudioClip startSound;
    [Tooltip("기기 작동 중에 반복 재생할 사운드")]
    public AudioClip processingLoopSound;
    [Tooltip("기기 작동 완료 시 재생할 사운드")]
    public AudioClip completeSound;

    [Header("완료 효과 설정")]
    [Tooltip("완료 시 모델이 깜빡이는 횟수")]
    public int blinkCount = 3;
    [Tooltip("깜빡이는 속도 (초 단위)")]
    public float blinkInterval = 0.5f;

    [Header("이벤트")]
    public UnityEvent OnProcessCompleted;
    public UnityEvent OnProcessStarted;
    private bool isLocked = true;

    // 내부 상태 변수
    private AudioSource audioSource;
    private GameObject itemInRange;
    private GameObject itemToProcess; // 처리할 아이템을 저장하는 변수 추가
    private GameObject currentlyProcessingItem;
    private XRGrabInteractable itemInteractable;
    private bool uiVisible = false;
    private bool handInRange = false;
    private bool readyToProcess = false; // 처리 준비 상태 추가
    private GameObject completedItem;
    private bool isJustCompleted = false;

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

        if (type == EquipmentType.AirIncubator && processingLight != null)
        {
            processingLight.enabled = false;
        }
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

        // 손이 닿았는데 기계가 잠겨있다면, 안내 문구만 표시하고 종료합니다.
        if (isLocked)
        {
            // 손이 닿았거나, ExperimentItem_L 스크립트를 가진 아이템이 닿았을 때
            if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand") || other.GetComponent<ExperimentItem_L>() != null)
            {
                if (statusText != null)
                {
                    statusText.text = "이전 실험 단계를 먼저 진행해주세요!";
                }
                SetUIVisible(true, false);
            }
            return; // 잠겨있으면 아래 로직은 실행하지 않음
        }

        // 이 스크립트가 비활성화 상태라면, 그 어떤 상호작용도 시작하지 않습니다.
        if (!this.enabled)
        {
            return;
        }

        ExperimentItem_L experimentItem = other.GetComponent<ExperimentItem_L>();

        if (isHand)
        {
            handInRange = true;
            Debug.Log($"{gameObject.name}: 손 '{other.tag}' 감지됨");
            UpdateInteractionUI();
        }
        else if (experimentItem != null)
        {
            // '특정 아이템 요구'가 체크되어 있을 때만 이 로직이 작동합니다.
            if (requireSpecificItemType)
            {
                // 들어온 아이템의 종류가 이 기계가 요구하는 종류와 다르면,
                if (experimentItem.itemType != requiredItemType)
                {
                    // 유효하지 않은 아이템으로 간주하고 무시합니다.
                    statusText.text = "올바른 샘플을 넣어주세요!";
                    Debug.LogWarning($"{gameObject.name}은(는) '{requiredItemType}' 타입의 아이템이 필요하지만, '{experimentItem.itemType}'이(가) 들어왔습니다. 무시합니다.");
                    return;
                }
            }

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
            isJustCompleted = false;
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
        // 1. 완료 상태: 깜빡이면서 사용자 확인을 기다립니다.
        if (currentState == MachineState.Complete)
        {
            UpdateStatusText(false);
            SetUIVisible(true, false);

            if (handInRange && !isJustCompleted && completedItem != null)
            {
                Debug.Log("사용자 확인 완료. 아이템을 배출합니다.");
                StopAllCoroutines();
                if (idleModelObject != null) idleModelObject.SetActive(true);
                if (processingModelObject != null) processingModelObject.SetActive(false);
                if (completedItemPlacementPoint != null)
                {
                    completedItem.transform.position = completedItemPlacementPoint.position;
                    completedItem.transform.rotation = completedItemPlacementPoint.rotation;
                }
                UnlockItem(completedItem);
                completedItem = null;
            }
            return;
        }

        // 2. 처리 중 상태: 손이 닿으면 "처리 중..." 문구를 표시합니다.
        if (currentState == MachineState.Processing)
        {
            if (statusText != null)
            {
                // ProcessTimer가 초를 업데이트하므로, 여기서는 간단한 텍스트만 표시합니다.
                statusText.text = "처리 중...";
            }
            SetUIVisible(true, false); // UI를 화면에 표시합니다.
            return; // 처리 중 로직은 여기서 끝.
        }

        // 3. 대기 상태: 아이템을 손에 쥐고 기기와 상호작용합니다.
        bool hasItemInHand = false;
        if (itemInRange != null && itemInteractable != null)
        {
            hasItemInHand = itemInteractable.isSelected;
        }

        if (hasItemInHand)
        {
            itemToProcess = itemInRange;
            readyToProcess = true;
            UpdateStatusText(true);

            SetUIVisible(true, true);
        }
        else if (itemInRange != null)
        {
            readyToProcess = false;
            itemToProcess = null;
            statusText.text = "샘플을 가져오세요.";
            SetUIVisible(true, false);
        }
        else
        {
            readyToProcess = false;
            itemToProcess = null;
            UpdateStatusText(false);
            SetUIVisible(true, false);
        }
    }

    public void StartProcessing()
    {
        StopAllCoroutines();

        if (!readyToProcess || itemToProcess == null || currentState != MachineState.Idle)
        {
            //Debug.LogWarning($"{gameObject.name}: 처리 시작 실패 - 조건 미충족");
            //Debug.LogWarning($"  실패 이유: readyToProcess={readyToProcess}, itemToProcess={(itemToProcess != null ? "있음" : "NULL")}, currentState={currentState}");
            return;
        }

        currentlyProcessingItem = itemToProcess;
        OnProcessStarted.Invoke();

        //Debug.Log($"{gameObject.name}: 처리 시작 - 아이템: {itemToProcess.name}");
        currentState = MachineState.Processing;

        // --- 사운드 및 모델 변경 로직 (기존과 동일) ---
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
            if (startSound != null) audioSource.PlayOneShot(startSound);
            if (processingLoopSound != null) StartCoroutine(PlayLoopSoundAfterDelay(startSound != null ? startSound.length : 0f));
        }
        if (idleModelObject != null) idleModelObject.SetActive(false);
        if (processingModelObject != null) processingModelObject.SetActive(true);

        SetUIVisible(false, false);

        GameObject processingItem = itemToProcess;
        XRGrabInteractable processingInteractable = processingItem.GetComponent<XRGrabInteractable>();
        ForceDropItem(processingItem, processingInteractable);
        LockItemInPlace(processingItem);

        readyToProcess = false;
        itemToProcess = null;

        // 1. '무한 작동'이 체크된 경우
        if (processInfinitely)
        {
            Debug.Log($"{gameObject.name}: 무한 작동을 시작합니다. (외부에서 MakeItemAvailable() 호출 필요)");
            // 타이머 코루틴을 시작하지 않고, Processing 상태를 유지합니다.
        }
        // 2. 처리 시간이 0초 이하인 경우 (즉시 완료)
        else if (processingTime <= 0)
        {
            Debug.Log($"{gameObject.name}: 처리 시간이 0이므로 즉시 완료합니다.");
            ProcessComplete(processingItem);
        }
        // 3. 일반적인 시간제 작동
        else
        {
            if (type == EquipmentType.ShakingIncubator)
            {
                StartCoroutine(AnimateLiquidChange(processingItem));
            }
            else
            {
                StartCoroutine(ProcessTimer(processingItem, debugProcessingTime));
            }
        }

        if (type == EquipmentType.AirIncubator && debugMode)
        {
            Debug.Log($"{gameObject.name}: 디버그 모드로 {debugProcessingTime}초 작동을 시작합니다.");
            if (processingLight != null) processingLight.enabled = true; // 조명 켜기
            StartCoroutine(ProcessTimer(processingItem, debugProcessingTime)); // 디버그 시간으로 타이머 시작
        }
        // 2. '무한 작동' 모드 (Air Incubator의 기본 모드)
        else if (processInfinitely)
        {
            Debug.Log($"{gameObject.name}: 무한 작동을 시작합니다. (씬 재시작 또는 외부 호출 필요)");
            if (type == EquipmentType.AirIncubator && processingLight != null) processingLight.enabled = true; // 조명 켜기
            // 타이머 코루틴을 시작하지 않고, Processing 상태를 유지합니다.
        }
        // 3. 처리 시간이 0초 이하인 경우 (즉시 완료)
        else if (processingTime <= 0)
        {
            Debug.Log($"{gameObject.name}: 처리 시간이 0이므로 즉시 완료합니다.");
            ProcessComplete(processingItem);
        }
        // 4. 일반적인 시간제 작동
        else
        {
            if (type == EquipmentType.ShakingIncubator)
            {
                StartCoroutine(AnimateLiquidChange(processingItem));
            }
            else
            {
                StartCoroutine(ProcessTimer(processingItem, processingTime)); // 일반 시간으로 타이머 시작
            }
        }
    }

    private IEnumerator ProcessTimer(GameObject targetItem, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            if (statusText != null)
            {
                statusText.text = $"처리 중... {Mathf.RoundToInt(duration - elapsedTime)}초";
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        ProcessComplete(targetItem);
    }

    private void ProcessComplete(GameObject targetItem)
    {
        if (type == EquipmentType.AirIncubator && processingLight != null)
        {
            processingLight.enabled = false;
        }

        Debug.Log($"{gameObject.name}: 처리 완료. 사용자 확인 대기 중...");
        currentState = MachineState.Complete;
        isJustCompleted = true; // '방금 완료됨' 상태로 설정
        completedItem = targetItem; // 완료된 아이템 저장

        // 완료 알림 사운드 재생
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            if (completeSound != null)
            {
                audioSource.PlayOneShot(completeSound);
            }
        }

        // 아이템 이동 및 잠금 해제 로직은 여기서 제거하고,
        // 대신 깜빡임 효과를 시작합니다.
        StartCoroutine(BlinkEffectCoroutine());

        // 시각적 결과 처리 (예: GelDoc 스크린)
        HandleVisualResult(targetItem);

        // 실험 흐름 관리 이벤트 호출
        OnProcessCompleted.Invoke();
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
                statusText.text = "샘플을 올려주세요.";
                break;
            case MachineState.Processing:
                statusText.text = "처리 중...";
                break;
            case MachineState.Complete:
                statusText.text = "완료. 샘플을 회수하세요.";
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
        // 장비 타입이 GelDoc일 경우 특별한 로직을 실행합니다.
        if (type == EquipmentType.GelDoc)
        {
            // RawImage와 ResultManager가 모두 정상적으로 연결되었는지 확인합니다.
            if (resultRawImage != null && ResultManager_L.Instance != null)
            {
                // ResultManager에서 랜덤 PCR 결과 텍스처를 가져와
                // resultRawImage의 텍스처에 적용합니다.
                resultRawImage.texture = ResultManager_L.Instance.GetRandomPcrResult();

                // 이미지가 확실히 보이도록 RawImage를 활성화합니다.
                resultRawImage.gameObject.SetActive(true);
                Debug.Log("성공: GelDoc RawImage에 결과 이미지를 적용했습니다.");
            }
            else
            {
                Debug.LogError("오류: resultRawImage 또는 ResultManager_L.Instance가 연결되지 않았습니다!");
            }
        }
    }

    private IEnumerator AnimateLiquidChange(GameObject targetItem)
    {
        // 1. 플라스크에 부착된 컨트롤러 스크립트를 찾아옵니다.
        FlaskLiquidController_G flaskController = targetItem.GetComponent<FlaskLiquidController_G>();
        if (flaskController == null || flaskController.liquidRenderer == null)
        {
            Debug.LogError("오류: 플라스크에서 FlaskLiquidController_G 또는 liquidRenderer를 찾을 수 없습니다. 일반 타이머로 대체 실행합니다.");
            // 문제가 생겼으니, 색상 변경 없이 일반 타이머로만 작동시킵니다.
            yield return StartCoroutine(ProcessTimer(targetItem, debugProcessingTime));
            yield break; // 코루틴을 여기서 종료합니다.
        }

        // 2. 플라스크 스크립트에서 필요한 정보(머티리얼, 색상)를 가져옵니다.
        Material liquidMaterial = flaskController.liquidRenderer.material;
        Color startLiquidColor = liquidMaterial.GetColor("_LiquidColor");
        Color startFresnelColor = liquidMaterial.GetColor("_FresnelColor");

        // 목표 색상은 플라스크 스크립트의 cloudyLiquidColor 변수에서 가져옵니다.
        Color targetLiquidColor = flaskController.cloudyLiquidColor;
        // 팀원의 스크립트와 동일한 시각적 효과를 위해 Fresnel 색상도 계산합니다.
        Color targetFresnelColor = new Color(targetLiquidColor.r - 0.05f, targetLiquidColor.g - 0.05f, targetLiquidColor.b);

        Debug.Log("Shaking Incubator: 플라스크 액체 색상 변경을 시작합니다.");

        // 3. 기계의 processingTime 동안 색상을 서서히 변경합니다.
        float elapsedTime = 0f;
        while (elapsedTime < processingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / processingTime; // 0에서 1까지의 진행률

            // Lerp 함수를 이용해 시작 색상에서 목표 색상으로 점진적으로 변경
            liquidMaterial.SetColor("_LiquidColor", Color.Lerp(startLiquidColor, targetLiquidColor, t));
            liquidMaterial.SetColor("_FresnelColor", Color.Lerp(startFresnelColor, targetFresnelColor, t));

            yield return null; // 다음 프레임까지 대기
        }

        // 4. 색상 변경이 끝나면, 기계의 처리를 완료합니다.
        Debug.Log("Shaking Incubator: 색상 변경 완료. 기기 처리를 종료합니다.");
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

    private IEnumerator BlinkEffectCoroutine()
    {
        while (true) // for문 대신 무한 루프로 변경
        {
            // 1. On 모델 켜기 / Off 모델 끄기
            if (idleModelObject != null) idleModelObject.SetActive(false);
            if (processingModelObject != null) processingModelObject.SetActive(true);
            yield return new WaitForSeconds(blinkInterval);

            // 2. On 모델 끄기 / Off 모델 켜기
            if (idleModelObject != null) idleModelObject.SetActive(true);
            if (processingModelObject != null) processingModelObject.SetActive(false);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    public void MakeItemAvailable()
    {
        if (currentState == MachineState.Processing && currentlyProcessingItem != null)
        {
            // 타이머를 즉시 종료하고 완료 프로세스를 강제로 실행합니다.
            StopAllCoroutines();
            ProcessComplete(currentlyProcessingItem);
            currentlyProcessingItem = null;
        }
    }

    public void SetLockState(bool lockState)
    {
        isLocked = lockState;
    }
}