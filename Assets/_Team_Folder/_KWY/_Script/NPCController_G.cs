using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
public class NpcController_G : MonoBehaviour
{
    public enum NPCState { Greeting, Observing, Listening, Processing, Responding }

    [Header("핵심 연결 컴포넌트")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform interestTargetTransform;

    [Header("UI 설정")]
    [SerializeField] private TMP_Text subtitleDisplay;
    [SerializeField] private float subtitleSentenceDuration = 4f;
    [SerializeField] private int maxCharactersPerSubtitle = 20;

    [Header("움직임 및 행동 설정")]
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float lookAtThreshold = 0.8f;
    [SerializeField] private float boredTimeout = 120f;

    private VoiceConversationManager_G voiceManager;
    private Animator npcAnimator;
    private NavMeshAgent navMeshAgent;
    private NPCState currentState;
    private float timeInCurrentState = 0f;
    private Coroutine currentStateCoroutine;

    #region Unity Lifecycle & FSM Core
    private void Awake()
    {
        if (!TryGetComponent(out voiceManager))
            Debug.LogWarning("NpcController_G ] VoiceConversationManager_G 없음");

        if (!TryGetComponent(out npcAnimator))
            Debug.LogWarning("NpcController_G ] Animator 없음");

        if (!TryGetComponent(out navMeshAgent))
            Debug.LogWarning("NpcController_G ] NavMeshAgent 없음");

        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = "";
            subtitleDisplay.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (voiceManager != null)
        {
            voiceManager.OnProcessingStarted += OnProcessingStarted;
            voiceManager.OnResponseReceived += OnResponseReceived;
        }
    }

    private void OnDisable()
    {
        if (voiceManager != null)
        {
            voiceManager.OnProcessingStarted -= OnProcessingStarted;
            voiceManager.OnResponseReceived -= OnResponseReceived;
        }
    }

    private void Start()
    {
        ChangeState(NPCState.Greeting);
    }

    private void Update()
    {
        timeInCurrentState += Time.deltaTime;
    }

    private void ChangeState(NPCState newState)
    {
        if (currentState == newState && currentStateCoroutine != null) return;

        // 이전에 실행되던 상태 코루틴이 있다면 중지
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        currentState = newState;
        timeInCurrentState = 0f;
        Debug.Log($"[NpcController] 상태 변경 -> {newState}");

        switch (currentState)
        {
            case NPCState.Greeting:
                currentStateCoroutine = StartCoroutine(Greeting_co());
                break;
            case NPCState.Observing:
                currentStateCoroutine = StartCoroutine(Observing_co());
                break;
            case NPCState.Listening:
                currentStateCoroutine = StartCoroutine(Listening_co());
                break;
            case NPCState.Processing:
                currentStateCoroutine = StartCoroutine(Processing_co());
                break;
            case NPCState.Responding:
                // Responding 상태는 OnResponseReceived에서 직접 코루틴을 호출하므로 여기서는 비워둠
                break;
        }
    }
    #endregion

    #region State Coroutines (각 상태의 행동을 정의하는 코루틴)
    private IEnumerator Greeting_co()
    {
        SetAnimatorFace("Default");

        Vector3 destination = playerTransform.position + (transform.position - playerTransform.position).normalized * followDistance;
        navMeshAgent.SetDestination(destination);

        yield return new WaitUntil(() => !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance);

        float timer = 0f;
        while (timer < 1f)
        {
            LookAtTarget(playerTransform);
            timer += Time.deltaTime;
            yield return null;
        }

        Queue<string> greeting = new Queue<string>();
        greeting.Enqueue("안녕하세요, AI 조수 노아입니다.");
        greeting.Enqueue("오늘은 어떤 실험을 도와드릴까요?");

        yield return StartCoroutine(ProcessSubtitleQueue_co(greeting));

        ChangeState(NPCState.Observing);
    }

    private IEnumerator Observing_co()
    {
        SetAnimatorFace("Default");

        while (true)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > followDistance) navMeshAgent.SetDestination(playerTransform.position);
            else navMeshAgent.ResetPath();

            Vector3 directionToNpc = (transform.position - playerTransform.position).normalized;
            float dotProduct = Vector3.Dot(playerTransform.forward, directionToNpc);
            if (dotProduct > lookAtThreshold) LookAtTarget(playerTransform);
            else LookAtTarget(interestTargetTransform);

            if (timeInCurrentState > boredTimeout) 
                SetAnimatorFace("Idle");

            yield return null;
        }
    }

    private IEnumerator Listening_co()
    {
        SetAnimatorFace("Default");
        navMeshAgent.ResetPath();

        while (true)
        {
            LookAtTarget(playerTransform);
            yield return null;
        }
    }

    private IEnumerator Processing_co()
    {
        SetAnimatorFace("Thinking");
        // 이 상태에서는 특별한 행동 없이 다음 신호(OnResponseReceived)를 기다림
        yield return null;
    }

    private IEnumerator ProcessSubtitleQueue_co(Queue<string> sentences)
    {
        if (subtitleDisplay == null) yield break;

        Queue<string> processedQueue = new Queue<string>();

        foreach (var sentence in sentences)
        {
            if (sentence.Length > maxCharactersPerSubtitle)
            {
                var splitSentences = SplitLongSentence(sentence, maxCharactersPerSubtitle);
                foreach (var part in splitSentences)
                {
                    processedQueue.Enqueue(part);
                }
            }
            else
            {
                processedQueue.Enqueue(sentence);
            }
        }

        while (processedQueue.Count > 0)
        {
            string nextSubtitle = processedQueue.Dequeue();
            subtitleDisplay.text = nextSubtitle;
            subtitleDisplay.gameObject.SetActive(true);
            yield return new WaitForSeconds(subtitleSentenceDuration);
        }

        subtitleDisplay.gameObject.SetActive(false);
        subtitleDisplay.text = "";
    }

    private List<string> SplitLongSentence(string sentence, int maxLength)
    {
        var parts = new List<string>();
        int currentIndex = 0;

        while (currentIndex < sentence.Length)
        {
            if (currentIndex + maxLength >= sentence.Length)
            {
                parts.Add(sentence.Substring(currentIndex));
                break;
            }

            int splitIndex = sentence.LastIndexOf(' ', currentIndex + maxLength);

            if (splitIndex <= currentIndex)
            {
                splitIndex = currentIndex + maxLength;
            }

            parts.Add(sentence.Substring(currentIndex, splitIndex - currentIndex).Trim());
            currentIndex = splitIndex + 1;
        }
        return parts;
    }
    #endregion

    #region Public Methods & Event Handlers
    public void OnPlayerStartsConversation()
    {
        if (currentState == NPCState.Observing)
        {
            ChangeState(NPCState.Listening);
            voiceManager.StartListening();
        }
    }

    public void OnProcessingStarted()
    {
        if (currentState == NPCState.Listening)
        {
            ChangeState(NPCState.Processing);
        }
    }

    public void OnResponseReceived(Queue<string> sentences, string rawResponse)
    {
        if (currentStateCoroutine != null) StopCoroutine(currentStateCoroutine);
        currentStateCoroutine = StartCoroutine(Responding_co(sentences, rawResponse));
    }

    private IEnumerator Responding_co(Queue<string> sentences, string rawResponse)
    {
        currentState = NPCState.Responding;
        timeInCurrentState = 0f;
        Debug.Log($"[NpcController] 상태 변경 -> {currentState}");

        if (rawResponse.Contains("성공")) 
            SetAnimatorFace("Success");
        else if (rawResponse.Contains("실패") || rawResponse.Contains("오류")) 
            SetAnimatorFace("Error");
        else 
            SetAnimatorFace("Default");

        yield return StartCoroutine(ProcessSubtitleQueue_co(sentences));

        ChangeState(NPCState.Observing);
    }
    #endregion

    private void LookAtTarget(Transform target)
    {
        if (target == null) return;
        Vector3 lookPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void SetAnimatorFace(string faceName)
    {
        if (npcAnimator == null) return;

        if (faceName == "Default") return;

        string triggerName = "";
        switch (faceName)
        {
            case "Thinking":
                triggerName = "Thinking";
                break;
            case "Success":
                triggerName = "Success";
                break;
            case "Failure":
                triggerName = "Error";
                break;
            case "Bored":
                triggerName = "Idle";
                break;
            default:
                return;
        }

        Debug.Log($"[NpcController] Animator Trigger 발동: {triggerName}");
        npcAnimator.SetTrigger(triggerName);
    }
}