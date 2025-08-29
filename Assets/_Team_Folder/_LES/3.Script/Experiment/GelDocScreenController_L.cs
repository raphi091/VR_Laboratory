using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GelDocScreenController_L : MonoBehaviour
{
    private enum GelDocState { Inactive, Analyzing, ReadyToShow, Displaying }
    private GelDocState currentState = GelDocState.Inactive;

    [Header("연결 요소")]
    public RawImage resultRawImage;
    public GameObject interactionUI;
    public TextMeshProUGUI interactionText;
    public Button yesButton;
    private CanvasGroup interactionCanvasGroup;

    [Header("설정")]
    [Tooltip("샘플을 읽는 데 걸리는 시간(초)")]
    public float analysisTime = 3.0f;
    
    [Header("사운드 설정")]
    [Tooltip("분석 완료 후, 확인 UI가 나타날 때 재생 (컴퓨터 부팅음)")]
    public AudioClip computerBootSound;
    [Tooltip("손이 기기에 가까이 갔을 때 재생 (키보드 타이핑 소리)")]
    public AudioClip keyboardSound;
    [Tooltip("결과 보기를 중단할 때 재생 (컴퓨터 종료음)")]
    public AudioClip computerShutdownSound;
    private AudioSource audioSource;

    [Header("이벤트")]
    public UnityEvent OnViewingStopped;

    private bool isHandInRange = false;
    private Texture pendingResultTexture;
    private bool isLocked = true;

    void Awake()
    {
        if (interactionUI != null)
        {
            interactionCanvasGroup = interactionUI.GetComponent<CanvasGroup>();
            if (interactionCanvasGroup == null)
            {
                Debug.LogError("오류: interactionUI 오브젝트에 CanvasGroup 컴포넌트가 없습니다!");
            }
        }
        // 시작 시 모든 UI 숨김
        resultRawImage.gameObject.SetActive(false);
        SetInteractionUIVisible(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void SetLockState(bool lockState)
    {
        isLocked = lockState;
//        Debug.Log($"장비 '{name}' 상태 변경: {(isLocked ? "잠김" : "활성화")}");
    }

    // 1. 분석 시작
    public void StartAnalysis()
    {
        currentState = GelDocState.Analyzing;
        StartCoroutine(AnalysisCoroutine());
    }

    // 2. 분석 딜레이 코루틴
    private IEnumerator AnalysisCoroutine()
    {
        yield return new WaitForSeconds(analysisTime);
        
        if (ResultManager_L.Instance != null)
        {
            pendingResultTexture = ResultManager_L.Instance.GetRandomPcrResult();
        }

        currentState = GelDocState.ReadyToShow;

        if (audioSource != null && computerBootSound != null)
        {
            audioSource.PlayOneShot(computerBootSound);
        }

        // 분석이 끝난 시점에, 만약 손이 이미 범위 안에 있다면 UI를 바로 갱신해줍니다.
        if (isHandInRange)
        {
            UpdateInteractionUI();
        }
    }

    // 3. 결과 표시
    public void ShowStoredResult()
    {
        if (pendingResultTexture != null)
        {
            resultRawImage.texture = pendingResultTexture;
            resultRawImage.gameObject.SetActive(true);
            currentState = GelDocState.Displaying;
            // 결과를 표시한 후에는, 손을 떼었다 다시 댈 때 UI가 갱신되도록 합니다.
            UpdateInteractionUI();
        }
    }

    // 4. 보기 중단
    public void HideResult()
    {
        if (audioSource != null && computerShutdownSound != null)
        {
            audioSource.PlayOneShot(computerShutdownSound);
        }

        resultRawImage.gameObject.SetActive(false);
        SetInteractionUIVisible(false);
        currentState = GelDocState.Inactive;
        OnViewingStopped.Invoke();
        //Debug.Log("GelDoc: 결과 보기 중단.");
    }

    // 상태에 맞는 UI를 설정하고 보여주는 로직
    private void UpdateInteractionUI()
    {
        // 손이 범위 밖에 있다면 무조건 UI를 숨깁니다.
        if (!isHandInRange)
        {
            SetInteractionUIVisible(false);
            return;
        }

        // 손이 범위 안에 있을 때만 상태에 맞는 UI를 표시합니다.
        switch (currentState)
        {
            case GelDocState.Analyzing:
                interactionText.text = "샘플 분석 중...";
                yesButton.gameObject.SetActive(false);
                SetInteractionUIVisible(true);
                break;

            case GelDocState.ReadyToShow:
                interactionText.text = "결과를 확인하시겠습니까?";
                yesButton.gameObject.SetActive(true); // << 버튼 활성화
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(ShowStoredResult);
                SetInteractionUIVisible(true);
                break;

            case GelDocState.Displaying:
                interactionText.text = "결과 그만보기";
                yesButton.gameObject.SetActive(true); // << 버튼 활성화
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(HideResult);
                SetInteractionUIVisible(true);
                break;
            
            default:
                SetInteractionUIVisible(false);
                break;
        }
    }

    // CanvasGroup 제어 함수
    private void SetInteractionUIVisible(bool isVisible)
    {
        if (interactionCanvasGroup != null)
        {
            interactionCanvasGroup.alpha = isVisible ? 1f : 0f;
            interactionCanvasGroup.interactable = isVisible;
            interactionCanvasGroup.blocksRaycasts = isVisible;
        }
        else if (interactionUI != null)
        {
            interactionUI.SetActive(isVisible);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            // 기계가 잠겨있다면, 안내 문구만 표시하고 더 이상 진행하지 않습니다.
            if (isLocked)
            {
                interactionText.text = "Gel Electrophoresis에 샘플을 넣어주세요.";
                yesButton.gameObject.SetActive(false);
                SetInteractionUIVisible(true);
                return;
            }

            // 잠겨있지 않을 때만 정상 로직 실행
            isHandInRange = true;
            if (currentState == GelDocState.ReadyToShow || currentState == GelDocState.Displaying)
            {
                if (audioSource != null && keyboardSound != null)
                {
                    audioSource.PlayOneShot(keyboardSound);
                }
            }
            UpdateInteractionUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            isHandInRange = false;

            // 잠겨있을 때는 손이 떠나면 안내 문구를 그냥 숨깁니다.
            if (isLocked)
            {
                SetInteractionUIVisible(false);
                return;
            }

            // 잠겨있지 않을 때만 정상 로직 실행
            UpdateInteractionUI();
        }
    }
}