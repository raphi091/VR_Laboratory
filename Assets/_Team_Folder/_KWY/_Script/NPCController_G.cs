using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcController_G : MonoBehaviour
{
    public enum NPCState
    {
        None,
        Greeting,
        WaitingForChoice,
        ExecutingExperiment,
        Processing,
        Finishing
    }

    [Header("플레이어")]
    [SerializeField] private Transform playerTransform;

    [Header("실험 데이터 및 목표물")]
    [Tooltip("PCR 실험 데이터를 연결합니다.")]
    [SerializeField] private ExperimentData pcrExperiment;
    [Tooltip("배양 실험 데이터를 연결합니다.")]
    [SerializeField] private ExperimentData cultureExperiment;
    [Tooltip("NPC가 평소에 바라볼 실험대 등의 Transform을 연결합니다.")]
    [SerializeField] private Transform interestTargetTransform;

    [Header("UI 설정")]
    [SerializeField] private TMP_Text subtitleDisplay;
    [SerializeField] private float subtitleSentenceDuration = 4f;
    [SerializeField] private int maxCharactersPerSubtitle = 40;

    [Header("행동 설정")]
    [SerializeField] private float approachDistance = 2.5f;
    [SerializeField] private float arrivalDistance = 3.0f;
    [SerializeField] private float lookAtThreshold = 0.8f;
    [SerializeField] private float boredTimeout = 120f;

    private VoiceConversationManager_G voiceManager;
    private Animator npcAnimator;
    private NavMeshAgent navMeshAgent;
    private NPCState currentState = NPCState.None;
    private Coroutine currentStateCoroutine;
    private ExperimentData currentExperiment;
    private float timeInCurrentState = 0f;
    private bool isWaitingForTaskCompletion = false;

    #region Unity Lifecycle & FSM Core
    private void Awake()
    {
        if (!TryGetComponent(out voiceManager)) 
            Debug.LogWarning("NpcController ] VoiceConversationManager 없음");

        if (!TryGetComponent(out npcAnimator))
            Debug.LogWarning("NpcController ] Animator 없음");

        if (!TryGetComponent(out navMeshAgent)) 
            Debug.LogWarning("NpcController ] NavMeshAgent 없음");

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
            voiceManager.OnProcessingStarted += OnGeminiProcessingStarted;
            voiceManager.OnResponseReceived += OnGeminiResponseReceived;
            voiceManager.OnExperimentChosen += OnExperimentChosen;
            voiceManager.OnTaskCompleted += OnTaskCompleted;
            voiceManager.OnChoiceNotUnderstood += OnChoiceNotUnderstood;
        }
    }

    private void OnDisable()
    {
        if (voiceManager != null)
        {
            voiceManager.OnProcessingStarted -= OnGeminiProcessingStarted;
            voiceManager.OnResponseReceived -= OnGeminiResponseReceived;
            voiceManager.OnExperimentChosen -= OnExperimentChosen;
            voiceManager.OnTaskCompleted -= OnTaskCompleted;
            voiceManager.OnChoiceNotUnderstood -= OnChoiceNotUnderstood;
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

    public void ChangeState(NPCState newState)
    {
        if (currentState == newState && currentStateCoroutine != null) return;

        if (currentStateCoroutine != null) 
            StopCoroutine(currentStateCoroutine);

        currentState = newState;
        timeInCurrentState = 0f;
        Debug.Log($"[NpcController] 상태 변경 -> {newState}");

        switch (currentState)
        {
            case NPCState.Greeting:
                currentStateCoroutine = StartCoroutine(Greeting_co());
                break;
            case NPCState.WaitingForChoice:
                currentStateCoroutine = StartCoroutine(WaitingForChoice_co());
                break;
            case NPCState.ExecutingExperiment:
                currentStateCoroutine = StartCoroutine(ExecutingExperiment_co());
                break;
            case NPCState.Processing:
                currentStateCoroutine = StartCoroutine(Processing_co());
                break;
            case NPCState.Finishing:
                currentStateCoroutine = StartCoroutine(Finishing_co());
                break;
        }
    }
    #endregion

    #region State Coroutines
    private IEnumerator Greeting_co()
    {
        SetAnimatorTrigger("Default");
        Vector3 destination = playerTransform.position + playerTransform.forward * approachDistance;
        navMeshAgent.SetDestination(destination);

        yield return new WaitUntil(() => IsNavMeshAgentAtDestination());

        yield return StartCoroutine(ShowSubtitle_co("안녕하세요, AI 조수 노아입니다. 오늘은 어떤 실험을 도와드릴까요?"));

        ChangeState(NPCState.WaitingForChoice);
    }

    private IEnumerator WaitingForChoice_co()
    {
        SetAnimatorTrigger("Default");
        voiceManager.StartListeningForChoice();

        while (true)
        {
            LookAtTarget(playerTransform);

            if (timeInCurrentState > boredTimeout)
            {
                SetAnimatorTrigger("Bored");
                timeInCurrentState = 0f;
            }

            yield return null;
        }
    }

    private IEnumerator ExecutingExperiment_co()
    {
        for (int i = 0; i < currentExperiment.Actions.Length; i++)
        {
            NpcAction currentAction = currentExperiment.Actions[i];
            Debug.Log($"[NpcController] 행동 실행: {currentAction.Type} (단계: {i + 1}/{currentExperiment.Actions.Length})");

            switch (currentAction.Type)
            {
                case ActionType.Move:
                    navMeshAgent.SetDestination(currentAction.TargetTransform.position);
                    yield return new WaitUntil(() => IsNavMeshAgentAtDestination());
                    break;
                case ActionType.Speak:
                    yield return StartCoroutine(ShowSubtitle_co(currentAction.Instruction));
                    break;
                case ActionType.WaitForPlayer:
                    LookAtTarget(playerTransform);
                    yield return new WaitUntil(() => Vector3.Distance(transform.position, playerTransform.position) < arrivalDistance);
                    break;
                case ActionType.ListenForCompletion:
                    isWaitingForTaskCompletion = true;
                    voiceManager.StartListeningForTask(currentAction.CompletionKeywords);
                    yield return new WaitUntil(() => !isWaitingForTaskCompletion);
                    break;
            }

            yield return new WaitForSeconds(0.5f);
        }
        ChangeState(NPCState.Finishing);
    }

    private IEnumerator Processing_co()
    {
        SetAnimatorTrigger("Thinking");

        while (true)
        {
            LookAtTarget(playerTransform);
            yield return null;
        }
    }

    private IEnumerator Finishing_co()
    {
        bool isSuccess = !currentExperiment.ExperimentName.Contains("C");

        if (isSuccess)
        {
            SetAnimatorTrigger("Success");

            yield return StartCoroutine(ShowSubtitle_co("실험이 성공적으로 끝났습니다! 훌륭해요."));
        }
        else
        {
            SetAnimatorTrigger("Error");

            yield return StartCoroutine(ShowSubtitle_co("이런, 이번 샘플은 뭔가 잘못된 것 같네요. 실험에 실패했습니다."));
        }

        yield return new WaitForSeconds(3f);
        ChangeState(NPCState.Greeting);
    }
    #endregion

    #region Public Control Methods & Event Handlers
    public void OnPlayerInteraction()
    {
        voiceManager.HandlePlayerInteraction();
    }

    public void OnExperimentChosen(ExperimentData chosenExperiment)
    {
        if (currentState != NPCState.WaitingForChoice) return;

        currentExperiment = chosenExperiment;
        ChangeState(NPCState.ExecutingExperiment);
    }

    private void OnChoiceNotUnderstood()
    {
        if (currentState != NPCState.WaitingForChoice) return;

        StartCoroutine(RepeatChoiceRequest_co());
    }

    private IEnumerator RepeatChoiceRequest_co()
    {
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        yield return StartCoroutine(ShowSubtitle_co("죄송합니다. 잘 이해하지 못했어요. PCR 또는 배양 중에서 다시 말씀해주시겠어요?"));

        ChangeState(NPCState.WaitingForChoice);
    }

    public void OnTaskCompleted()
    {
        isWaitingForTaskCompletion = false;
    }

    public void OnFreeQuestionAsked()
    {
        if (currentState != NPCState.ExecutingExperiment || !isWaitingForTaskCompletion) return;

        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }
    }

    public void OnGeminiProcessingStarted()
    {
        ChangeState(NPCState.Processing);
    }

    public void OnGeminiResponseReceived(Queue<string> sentences, string rawResponse)
    {
        StartCoroutine(RespondToFreeQuestion_co(sentences, rawResponse));
    }

    private IEnumerator RespondToFreeQuestion_co(Queue<string> sentences, string rawResponse)
    {
        if (rawResponse.Contains("성공")) 
            SetAnimatorTrigger("Success");
        else if (rawResponse.Contains("실패") || rawResponse.Contains("오류")) 
            SetAnimatorTrigger("Error");
        else 
            SetAnimatorTrigger("Default");

        yield return StartCoroutine(ProcessSubtitleQueue_co(sentences));

        ChangeState(NPCState.ExecutingExperiment);
    }
    #endregion

    #region Helpers
    private IEnumerator ShowSubtitle_co(string fullText)
    {
        Queue<string> q = new Queue<string>();
        q.Enqueue(fullText);

        yield return StartCoroutine(ProcessSubtitleQueue_co(q));
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
                    processedQueue.Enqueue(part);
            }
            else
            {
                processedQueue.Enqueue(sentence);
            }
        }

        while (processedQueue.Count > 0)
        {
            subtitleDisplay.text = processedQueue.Dequeue();
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

    private void LookAtTarget(Transform target)
    {
        if (target == null) return;

        Vector3 lookPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (npcAnimator == null) return;

        if (triggerName == "Default") return;

        if (triggerName == "Bored") 
            npcAnimator.SetTrigger("Idle");
        else 
            npcAnimator.SetTrigger(triggerName);

        Debug.Log($"[NpcController] Animator Trigger 발동: {triggerName}");
    }

    private bool IsNavMeshAgentAtDestination()
    {
        if (!navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }

        return false;
    }
    #endregion
}