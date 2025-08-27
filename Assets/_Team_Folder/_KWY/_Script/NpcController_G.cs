using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;


[System.Serializable]
public class StatusIcon_G
{
    public string name;
    public Sprite sprite;
}

[RequireComponent(typeof(NavMeshAgent))]
public class NpcController_G : MonoBehaviour
{
    public enum NPCState
    {
        None,
        Greeting,
        WaitingForExperimentChoice,
        ExecutingExperiment,
        Processing,
        Finishing,
        FreeConversation,
        TutorialGreeting
    }

    public enum NpcMode
    {
        Tutorial,
        MainLab
    }

    [Header("모드 설정")]
    [Tooltip("NPC가 작동할 모드.")]
    [SerializeField] private NpcMode currentMode;

    [Header("플레이어")]
    [SerializeField] private Transform playerTransform;

    [Header("실험 데이터 및 목표물")]
    [Tooltip("PCR 실험 데이터.")]
    [SerializeField] private ExperimentData_G pcrExperiment;
    [Tooltip("배양 실험 데이터")]
    [SerializeField] private ExperimentData_G cultureExperiment;
    [Tooltip("튜토리얼 데이터")]
    [SerializeField] private ExperimentData_G tutorialExperiment;

    [Header("UI 설정")]
    [SerializeField] private TMP_Text subtitleDisplay;
    [SerializeField] private float subtitleSentenceDuration = 4f;
    [SerializeField] private int maxCharactersPerSubtitle = 40;
    [SerializeField] private GameObject choiceUIPanel;

    [Header("상태 표시 UI")]
    [Tooltip("상태 아이콘")]
    [SerializeField] private Image statusIconImage;
    [Tooltip("상태 아이콘 목록")]
    [SerializeField] private List<StatusIcon_G> statusIcons;

    [Header("행동 설정")]
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float arrivalDistance = 3.0f;
    [SerializeField] private float lookAtThreshold = 0.8f;
    [SerializeField] private float boredTimeout = 120f;

    private VoiceConversationManager_G voiceManager;
    private LocationManager_G locationManager;
    private Animator npcAnimator;
    private NavMeshAgent navMeshAgent;
    private NPCState currentState = NPCState.None;
    private NPCState previousStateBeforeQuestion;
    private Coroutine currentStateCoroutine;
    private Coroutine statusIconCoroutine;
    private ExperimentData_G currentExperiment;
    private SampleData_G currentSample;
    private int currentActionIndex;
    private int savedActionIndex;
    private float timeInCurrentState = 0f;
    private bool isWaitingForTaskCompletion = false;
    
    public event Action<NpcMode> OnExperimentEnd;
    

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

        if (choiceUIPanel != null)
        {
            choiceUIPanel.SetActive(false);
        }

        previousStateBeforeQuestion = NPCState.Greeting;
    }

    private void OnEnable()
    {
        if (voiceManager != null)
        {
            voiceManager.OnProcessingStarted += OnGeminiProcessingStarted;
            voiceManager.OnResponseReceived += OnGeminiResponseReceived;
            voiceManager.OnExperimentChosen += OnExperimentChosen;
            voiceManager.OnTaskCompleted += OnTaskCompleted;
            voiceManager.OnFreeQuestionAsked += OnFreeQuestionAsked;
            voiceManager.OnChoiceNotUnderstood += OnChoiceNotUnderstood;
            voiceManager.OnListeningStopped += HideStatusIcon;
            voiceManager.OnTutorialChosen += OnTutorialChosen;
            voiceManager.OnTutorialChoiceNotUnderstood += OnTutorialChoiceNotUnderstood;
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
            voiceManager.OnFreeQuestionAsked -= OnFreeQuestionAsked;
            voiceManager.OnChoiceNotUnderstood -= OnChoiceNotUnderstood;
            voiceManager.OnListeningStopped -= HideStatusIcon;
            voiceManager.OnTutorialChosen -= OnTutorialChosen;           
            voiceManager.OnTutorialChoiceNotUnderstood -= OnTutorialChoiceNotUnderstood;
        }
    }

    private void Start()
    {
        if (locationManager == null) 
            locationManager = FindObjectOfType<LocationManager_G>();

        switch (currentMode)
        {
            case NpcMode.Tutorial:
                if (tutorialExperiment != null)
                {
                    currentExperiment = tutorialExperiment;
                    currentSample = tutorialExperiment.Samples[0];
                    ChangeState(NPCState.TutorialGreeting);
                }
                else
                {
                    Debug.LogError("튜토리얼 데이터가 연결되지 않았습니다!");
                }
                break;
            case NpcMode.MainLab:
                if (pcrExperiment == null || cultureExperiment == null)
                {
                    ChangeState(NPCState.FreeConversation);
                }
                else
                {
                    ChangeState(NPCState.Greeting);
                }
                break;
        }
    }

    private void Update()
    {
        timeInCurrentState += Time.deltaTime;
    }

    public void ChangeState(NPCState newState, int startIndex = 0)
    {
        if (currentState == newState && currentStateCoroutine != null) return;

        if (currentStateCoroutine != null) 
            StopCoroutine(currentStateCoroutine);

        currentState = newState;
        timeInCurrentState = 0f;
        Debug.Log($"[NpcController] 상태 변경 -> {newState}");

        switch (currentState)
        {
            case NPCState.TutorialGreeting:
                currentStateCoroutine = StartCoroutine(Tut_Greeting_co());
                break;
            case NPCState.Greeting:
                currentStateCoroutine = StartCoroutine(Greeting_co());
                break;
            case NPCState.WaitingForExperimentChoice:
                currentStateCoroutine = StartCoroutine(WaitingForExperimentChoice_co());
                break;
            case NPCState.ExecutingExperiment:
                currentStateCoroutine = StartCoroutine(ExecutingExperiment_co(startIndex));
                break;
            case NPCState.Processing:
                currentStateCoroutine = StartCoroutine(Processing_co());
                break;
            case NPCState.Finishing:
                currentStateCoroutine = StartCoroutine(Finishing_co());
                break;
            case NPCState.FreeConversation:
                currentStateCoroutine = StartCoroutine(FreeConversation_co());
                break;
        }
    }
    #endregion

    #region State Coroutines
    
    private IEnumerator Tut_Greeting_co()
    {
        SetAnimatorTrigger("Default");
        Vector3 destination = playerTransform.position + playerTransform.forward * followDistance;
        navMeshAgent.SetDestination(destination);
        yield return new WaitUntil(() => IsNavMeshAgentAtDestination());

        yield return StartCoroutine(ShowSubtitle_co("안녕하세요, 여러분의 AI 비서 노아입니다. 실험실 튜토리얼에 오신 걸 환영합니다."));
        if (C_DataManager.I.gameData.IsTutorialCompleted)
        {
            yield return StartCoroutine(ShowSubtitle_co("튜토리얼을 이미 완료하신 상태입니다. 다시 튜토리얼을 진행하시겠나요?"));
            yield return StartCoroutine(ShowSubtitle_co("선택에 따라 예, 다시 진행하겠습니다. 혹은 아니오, 실험으로 넘어가겠습니다. 를 말해주세요."));
            voiceManager.StartListeningForTutorialChoice();
        }
        else
        {
            ChangeState(NPCState.ExecutingExperiment);
        }
    }
    
    private IEnumerator Greeting_co()
    {
        SetAnimatorTrigger("Default");
        Vector3 destination = playerTransform.position + playerTransform.forward * followDistance;
        navMeshAgent.SetDestination(destination);
        yield return new WaitUntil(() => IsNavMeshAgentAtDestination());

        yield return StartCoroutine(ShowSubtitle_co("안녕하세요, 노아입니다. 실험은 1번 PCR, 2번 배양이 준비되어 있습니다."));
        yield return StartCoroutine(ShowSubtitle_co("오늘은 무슨 실험을 하시겠습니까? 자유로운 대화를 원하시면 '자유 대화'라고 말씀해주세요."));

        ChangeState(NPCState.WaitingForExperimentChoice);
    }

    private IEnumerator WaitingForExperimentChoice_co()
    {
        SetAnimatorTrigger("Default");

        if (choiceUIPanel != null) 
            choiceUIPanel.SetActive(true);

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

    private IEnumerator RepeatChoiceRequest_co()
    {
        if (currentStateCoroutine != null)
            StopCoroutine(currentStateCoroutine);

        ShowStatusIcon("Question", 2f);
        yield return StartCoroutine(ShowSubtitle_co("죄송합니다. 잘 이해하지 못했어요. 다시 말씀해주시겠어요?"));
        

        ChangeState(NPCState.WaitingForExperimentChoice);
    }

    private IEnumerator ExecutingExperiment_co(int startIndex = 0)
    {
        for (int i = 0; i < currentSample.Actions.Length; i++)
        {
            currentActionIndex = i;
            NpcAction currentAction = currentSample.Actions[i];
            Debug.Log($"[NpcController] 행동 실행: {currentAction.Type} (단계: {i + 1}/{currentSample.Actions.Length})");

            switch (currentAction.Type)
            {
                case ActionType.Move:
                    Transform targetTransform = locationManager.GetLocation(currentAction.LocationID);
                    if (targetTransform != null)
                    {
                        navMeshAgent.SetDestination(targetTransform.position);
                        yield return new WaitUntil(() => IsNavMeshAgentAtDestination());
                    }
                    else
                    {
                        Debug.LogError($"LocationID '{currentAction.LocationID}'를 찾을 수 없습니다.");
                    }
                    break;
                case ActionType.Speak:
                    LookAtTarget(playerTransform);
                    yield return StartCoroutine(ShowSubtitle_co(currentAction.Instruction));
                    break;
                case ActionType.WaitForPlayer:
                    LookAtTarget(playerTransform);
                    yield return new WaitUntil(() => Vector3.Distance(transform.position, playerTransform.position) < arrivalDistance);
                    break;
                case ActionType.ListenForCompletion:
                    LookAtTarget(playerTransform);
                    isWaitingForTaskCompletion = true;
                    voiceManager.StartListeningForTask(currentAction.CompletionKeywords);
                    yield return new WaitUntil(() => !isWaitingForTaskCompletion);
                    break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        ChangeState(NPCState.Finishing);
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

        if (previousStateBeforeQuestion == NPCState.ExecutingExperiment)
        {
            ChangeState(previousStateBeforeQuestion, savedActionIndex);

        }
        else
        {
            ChangeState(previousStateBeforeQuestion);
        }
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

        if (currentMode == NpcMode.Tutorial)
        {
            ShowStatusIcon("Success", 2f);
            SetAnimatorTrigger("Success");
            yield return StartCoroutine(ShowSubtitle_co("튜토리얼을 성공적으로 마쳤습니다! 훌륭해요."));
        }
        else if (isSuccess)
        {
            ShowStatusIcon("Success", 2f);
            SetAnimatorTrigger("Success");
            yield return StartCoroutine(ShowSubtitle_co("실험이 성공적으로 끝났습니다! 훌륭해요."));
        }
        else
        {
            ShowStatusIcon("Error", 2f);
            SetAnimatorTrigger("Error");
            yield return StartCoroutine(ShowSubtitle_co("이런, 이번 샘플은 뭔가 잘못된 것 같네요. 실험에 실패했습니다."));
        }

        yield return new WaitForSeconds(3f);
        
        
        OnExperimentEnd?.Invoke(currentMode);
        ChangeState(NPCState.Greeting);
    }

    private IEnumerator FreeConversation_co()
    {
        SetAnimatorTrigger("Default");

        yield return StartCoroutine(ShowSubtitle_co("무엇이든 물어보세요. 대화를 시작하려면 버튼을 눌러주세요."));

        while (true)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer > followDistance) 
                navMeshAgent.SetDestination(playerTransform.position);

            else 
                navMeshAgent.ResetPath();

            LookAtTarget(playerTransform);

            yield return null;
        }
    }
    #endregion

    #region Public Control Methods & Event Handlers
    public void OnPlayerInteraction()
    {
        switch (currentState)
        {
            case NPCState.WaitingForExperimentChoice:
                ShowStatusIcon("Listening", 0);
                voiceManager.StartListeningForChoice();
                break;
            case NPCState.FreeConversation:
                voiceManager.StartListeningForTask(new List<string>());
                break;
            case NPCState.ExecutingExperiment:
                OnFreeQuestionAsked();
                voiceManager.StartListeningForTask(new List<string>());
                break;
            default:
                Debug.Log($"[NpcController] 현재 상태({currentState})에서는 상호작용이 불가능합니다.");
                break;
        }
    }

    public void OnExperimentChosen(ExperimentData_G chosenExperiment)
    {
        if (currentState != NPCState.WaitingForExperimentChoice) return;

        if (choiceUIPanel != null)
            choiceUIPanel.SetActive(false);

        if (chosenExperiment == null)
        {
            if (voiceManager.LastTranscription != null && voiceManager.LastTranscription.Contains("자유 대화"))
            {
                ChangeState(NPCState.FreeConversation);
            }
            else
            {
                StartCoroutine(RepeatChoiceRequest_co());
            }
            return;
        }
        currentExperiment = chosenExperiment;
        currentSample = chosenExperiment.Samples[0];
        ChangeState(NPCState.ExecutingExperiment);
    }

    public void OnTutorialChosen(bool isChosen)
    {
        if (isChosen)
        {
            ChangeState(NPCState.ExecutingExperiment);
        }
        else
        {
            OnExperimentEnd?.Invoke(NpcMode.Tutorial);
        }
    }

    private void OnChoiceNotUnderstood()
    {
        if (currentState != NPCState.WaitingForExperimentChoice) return;

        if (choiceUIPanel != null)
            choiceUIPanel.SetActive(false);

        StartCoroutine(RepeatChoiceRequest_co());
    }
    
    private void OnTutorialChoiceNotUnderstood()
    {
        if (currentState != NPCState.TutorialGreeting) return;
        
        StartCoroutine(Tut_Greeting_co());
    }

    public void OnTaskCompleted()
    {
        isWaitingForTaskCompletion = false;
    }

    public void OnFreeQuestionAsked()
    {
        if (currentState != NPCState.ExecutingExperiment && currentState != NPCState.FreeConversation) return;

        previousStateBeforeQuestion = currentState;
        savedActionIndex = currentActionIndex;

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
    #endregion

    #region Helpers
    private IEnumerator ShowSubtitle_co(string fullText)
    {
        if (subtitleDisplay == null) yield break;

        try
        {
            ShowStatusIcon("Speaking");
            Queue<string> q = new Queue<string>();
            q.Enqueue(fullText);
            yield return StartCoroutine(ProcessSubtitleQueue_co(q));
        }
        finally
        {
            HideStatusIcon();
        }
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

    private void ShowStatusIcon(string iconName, float duration = 0f)
    {
        if (statusIconImage == null) return;

        Sprite iconSprite = statusIcons.Find(icon => icon.name == iconName)?.sprite;

        if (iconSprite == null)
        {
            Debug.LogWarning($"[NpcController] '{iconName}' 이라는 이름의 아이콘을 찾을 수 없습니다.");
            HideStatusIcon();
            return;
        }

        if (statusIconCoroutine != null)
        {
            StopCoroutine(statusIconCoroutine);
        }

        statusIconCoroutine = StartCoroutine(ShowStatusIcon_co(iconSprite, duration));
    }

    private void HideStatusIcon()
    {
        if (statusIconImage == null) return;
        if (statusIconCoroutine != null)
        {
            StopCoroutine(statusIconCoroutine);
            statusIconCoroutine = null;
        }
        statusIconImage.enabled = false;
    }

    private IEnumerator ShowStatusIcon_co(Sprite icon, float duration)
    {
        statusIconImage.sprite = icon;
        statusIconImage.enabled = true;

        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            HideStatusIcon();
        }
    }
    #endregion
}