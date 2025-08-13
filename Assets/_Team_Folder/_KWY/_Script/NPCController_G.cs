using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;


[RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
public class NpcController_G : MonoBehaviour
{
    public enum NPCState
    {
        Greeting,
        Observing,
        Listening,
        Processing,
        Responding
    }

    [Header("핵심 연결 컴포넌트")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform interestTargetTransform;

    [Header("UI 설정")]
    [SerializeField] private TMP_Text subtitleDisplay;
    [SerializeField] private float subtitleSentenceDuration = 4f;

    [Header("움직임 및 행동 설정")]
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float lookAtThreshold = 0.8f;
    [SerializeField] private float boredTimeout = 120f;

    private VoiceConversationManager_G voiceManager;
    private NavMeshAgent navMeshAgent;
    private Animator npcAnimator;
    private NPCState currentState;
    private float timeInCurrentState = 0f;
    private Coroutine subtitleCoroutine;

    #region Unity Lifecycle & FSM Core
    private void Awake()
    {
        if (!TryGetComponent(out voiceManager))
            Debug.LogWarning("NPCController_G ] VoiceConversationManager 없음");

        if (!TryGetComponent(out navMeshAgent))
            Debug.LogWarning("NPCController_G ] NavMeshAgent 없음");

        if (!TryGetComponent(out npcAnimator))
            Debug.LogWarning("NPCController_G ] Animator 없음");

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

        switch (currentState)
        {
            case NPCState.Greeting:
                break;
            case NPCState.Observing:
                HandleObservingState();
                break;
            case NPCState.Listening:
            case NPCState.Processing:
                LookAtTarget(playerTransform);
                break;
            case NPCState.Responding:
                LookAtTarget(playerTransform);
                break;
        }
    }

    private void ChangeState(NPCState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        timeInCurrentState = 0f;
        Debug.Log($"[NpcController] 상태 변경 -> {newState}");

        switch (currentState)
        {
            case NPCState.Greeting:
                StartCoroutine(GreetingSequence());
                break;
            case NPCState.Observing:
                SetAnimatorFace("Default");
                break;
            case NPCState.Listening:
                SetAnimatorFace("Default");
                navMeshAgent.ResetPath();
                break;
            case NPCState.Processing:
                SetAnimatorFace("Thinking");
                break;
            case NPCState.Responding:
                SetAnimatorFace("Default");
                break;
        }
    }
    #endregion

    #region State Handlers
    private void HandleObservingState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > followDistance)
        {
            navMeshAgent.SetDestination(playerTransform.position);
        }
        else
        {
            navMeshAgent.ResetPath();
        }

        Vector3 directionToNpc = (transform.position - playerTransform.position).normalized;
        float dotProduct = Vector3.Dot(playerTransform.forward, directionToNpc);

        if (dotProduct > lookAtThreshold)
        {
            LookAtTarget(playerTransform);
        }
        else
        {
            LookAtTarget(interestTargetTransform);
        }

        if (timeInCurrentState > boredTimeout)
        {
            SetAnimatorFace("Bored");
        }
    }

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

        int stateId = 0; // Default
        if (faceName == "Thinking") stateId = 1;
        else if (faceName == "Success") stateId = 2;
        else if (faceName == "Failure") stateId = 3;
        else if (faceName == "Bored") stateId = 4;

        npcAnimator.SetInteger("FaceState", stateId);
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
        ChangeState(NPCState.Responding);

        if (rawResponse.Contains("성공")) SetAnimatorFace("Success");
        else if (rawResponse.Contains("실패") || rawResponse.Contains("오류")) SetAnimatorFace("Failure");

        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        subtitleCoroutine = StartCoroutine(ProcessSubtitleQueue(sentences));
    }
    #endregion

    #region Coroutines
    private IEnumerator GreetingSequence()
    {
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
        yield return StartCoroutine(ProcessSubtitleQueue(greeting));

        ChangeState(NPCState.Observing);
    }

    private IEnumerator ProcessSubtitleQueue(Queue<string> sentences)
    {
        if (subtitleDisplay == null) yield break;
        while (sentences.Count > 0)
        {
            string nextSubtitle = sentences.Dequeue();
            subtitleDisplay.text = nextSubtitle;
            subtitleDisplay.gameObject.SetActive(true);
            yield return new WaitForSeconds(subtitleSentenceDuration);
        }

        subtitleDisplay.gameObject.SetActive(false);
        subtitleDisplay.text = "";

        ChangeState(NPCState.Observing);
    }
    #endregion
}
