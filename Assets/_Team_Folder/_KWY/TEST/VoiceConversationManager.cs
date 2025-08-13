using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Newtonsoft.Json;
using Oculus.Voice;


#region Data_Structures
// Gemini API와 통신하기 위한 데이터 구조
[System.Serializable] public class GeminiRequest { public List<Content> contents; }
[System.Serializable] public class Content { public string role; public List<Part> parts; }
[System.Serializable] public class Part { public string text; }
[System.Serializable] public class GeminiResponse { public List<Candidate> candidates; }
[System.Serializable] public class Candidate { public Content content; }

// secrets.json 파일에서 API 키를 읽어오기 위한 구조
[System.Serializable] public class Secrets { public string apiKey; }
#endregion

public class VoiceConversationManager : MonoBehaviour
{
    [Header("핵심 연결 컴포넌트")]
    [Tooltip("씬에 있는 App Voice Experience 컴포넌트를 연결해주세요.")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;

    [Header("UI 설정")]
    [Tooltip("Gemini의 답변을 자막으로 표시할 TextMeshPro UI")]
    [SerializeField] private TMP_Text subtitleDisplay;
    [Tooltip("자막이 표시될 시간(초)")]
    [SerializeField] private float subtitleDisplayDuration = 5f;

    [Header("XR 입력 설정")]
    [Tooltip("음성 인식을 활성화할 입력 액션입니다.")]
    [SerializeField] private InputActionReference activateVoiceAction;

    // 비공개 변수들
    private string apiKey;
    private HttpClient httpClient;
    private readonly List<Content> conversationHistory = new List<Content>();
    private bool isLoading = false;
    private CancellationTokenSource cancellationTokenSource;
    private Coroutine subtitleCoroutine;

    private Queue<string> subtitleQueue = new Queue<string>();
    private bool isSubtitleShowing = false;


    #region Unity Lifecycle
    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "secrets.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            Secrets secrets = JsonUtility.FromJson<Secrets>(json);
            apiKey = secrets.apiKey;
        }
        else
        {
            Debug.LogError("secrets.json 파일을 찾을 수 없습니다! StreamingAssets 폴더를 확인해주세요.");
        }

        // --- HTTP 클라이언트 초기화 ---
        httpClient = new HttpClient { Timeout = System.TimeSpan.FromMinutes(5) };

        // --- 자막 UI 초기 상태 설정 ---
        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = "";
            subtitleDisplay.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionReceived);

            appVoiceExperience.VoiceEvents.OnSend.AddListener((_) => Debug.Log("음성 인식 요청 전송됨."));
            appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(() => Debug.Log("음성 인식 종료됨."));
        }

        activateVoiceAction.action.Enable();
    }

    private void OnDisable()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnTranscriptionReceived);
        }

        activateVoiceAction.action.Disable();
    }

    private void Start()
    {
        SetupInitialPrompt();
    }

    private void SetupInitialPrompt()
    {
        // AI 조수의 역할, 성격, 지식, 규칙 등을 상세하게 정의한 '설명서' 입니다.
        // 이 내용을 상세하게 작성할수록 AI가 더 똑똑하고 자연스러워집니다.
        string initialPrompt = @"
        당신은 '프로젝트 제네시스'를 돕는 AI 실험실 조수 '노아'입니다.
        당신의 임무는 사용자가 실험을 원활하게 진행할 수 있도록 돕는 것입니다.
        항상 친절하고 명확한 존댓말을 사용하며, 사용자를 격려하는 긍정적인 태도를 유지해야 합니다.
        당신은 아래의 핵심 안전 수칙과 실험 절차를 반드시 숙지하고 있어야 합니다.

        [핵심 안전 수칙]
        1. 모든 실험 전에는 반드시 보안경과 장갑을 착용해야 합니다.
        2. '물질 A'와 '물질 B'는 절대 직접적으로 혼합해서는 안 됩니다. 혼합 시 폭발 위험이 있습니다.
        3. 실험실을 나갈 때는 모든 전원을 반드시 차단해야 합니다.

        [기본 실험 절차: 에너지 결정체 생성]
        1. '안정화 용액' 100ml를 비커에 담습니다.
        2. '중화기'를 사용하여 용액의 온도를 정확히 50도로 맞춥니다.
        3. '물질 C' 가루 10g을 천천히 넣고 1분간 저어줍니다.
        4. 용액이 푸른 빛으로 변하면 실험이 성공한 것입니다.

        당신은 위 정보를 바탕으로 사용자의 질문에 답변해야 합니다.
        만약 모르는 질문을 받으면, '그 정보는 제 데이터베이스에 없습니다. 매뉴얼을 확인해보시겠어요?' 라고 솔직하게 답변해야 합니다.
        절대로 없는 정보를 지어내서 말하면 안 됩니다.
        ";

        // 대화 기록의 가장 처음에 이 '설명서'를 시스템의 지시사항으로 추가합니다.
        // 'user' 역할로 보내면, AI는 이 내용을 읽고 답변을 준비하는 상태가 됩니다.
        conversationHistory.Add(new Content { role = "user", parts = new List<Part> { new Part { text = initialPrompt } } });

        // 그리고 AI가 역할을 완벽하게 수락했다는 응답을 가상으로 추가해줍니다.
        // 이 과정을 통해 AI는 다음 사용자 질문부터 '노아' 역할에 완전히 몰입하게 됩니다.
        conversationHistory.Add(new Content { role = "model", parts = new List<Part> { new Part { text = "알겠습니다. 저는 AI 실험실 조수 노아입니다. 사용자의 안전과 성공적인 실험을 위해 최선을 다하겠습니다. 무엇을 도와드릴까요?" } } });

        Debug.Log("AI 조수 역할 설정 완료: 실험실 조수 노아");
    }

    private void Update()
    {
        // 로딩 중이 아닐 때, 지정된 입력 액션(컨트롤러 버튼)이 눌렸는지 확인합니다.
        if (!isLoading && activateVoiceAction.action.WasPressedThisFrame())
        {
            ToggleVoiceRecognition();
        }
    }

    private void OnDestroy()
    {
        // 앱 종료 시 리소스를 정리합니다.
        httpClient?.Dispose();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
    #endregion

    #region Core Logic
    private void ToggleVoiceRecognition()
    {
        if (appVoiceExperience.Active)
        {
            // 이미 활성화 상태이면 비활성화
            appVoiceExperience.Deactivate();
        }
        else
        {
            // 비활성화 상태이면 활성화
            appVoiceExperience.Activate();
        }
    }

    private void OnTranscriptionReceived(string transcribedText)
    {
        if (isLoading || string.IsNullOrWhiteSpace(transcribedText))
        {
            return; // 로딩 중이거나 인식된 텍스트가 없으면 무시
        }

        Debug.Log($"[STT 결과]: {transcribedText}");
        // 비동기 메서드를 호출합니다. _ = ... 구문은 '결과를 기다리지 않고 실행'을 의미합니다.
        _ = SendMessageToGeminiAsync(transcribedText);
    }

    private async Task SendMessageToGeminiAsync(string userMessage)
    {
        isLoading = true;

        // 대화 기록에 사용자 메시지 추가
        conversationHistory.Add(new Content { role = "user", parts = new List<Part> { new Part { text = userMessage } } });

        // API 요청 준비
        var requestData = new GeminiRequest { contents = conversationHistory };
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";
        var jsonContent = JsonConvert.SerializeObject(requestData);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri) { Content = httpContent })
            {
                // Gemini API에 요청 전송
                using (var response = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token))
                {
                    response.EnsureSuccessStatusCode();

                    // 응답 전체를 하나의 문자열로 읽어옵니다.
                    string fullJsonResponse = await response.Content.ReadAsStringAsync();

                    // 응답 파싱 및 텍스트 추출
                    var fullResponse = JsonConvert.DeserializeObject<GeminiResponse>(fullJsonResponse);
                    var modelResponseText = fullResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

                    if (!string.IsNullOrEmpty(modelResponseText))
                    {
                        Debug.Log($"[Gemini 답변]: {modelResponseText}");
                        // 대화 기록에 모델(Gemini)의 답변 추가
                        conversationHistory.Add(new Content { role = "model", parts = new List<Part> { new Part { text = modelResponseText } } });

                        // [수정된 부분] Gemini 답변을 문장 단위로 쪼개서 큐에 추가합니다.
                        EnqueueSubtitles(modelResponseText);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Gemini 오류]: {e.ToString()}");
            // 오류가 발생했을 때도 자막으로 표시하도록 EnqueueSubtitles를 호출할 수 있습니다.
            EnqueueSubtitles($"오류가 발생했습니다: {e.Message}");
        }
        finally
        {
            isLoading = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void EnqueueSubtitles(string fullText)
    {
        // 정규식을 사용하여 마침표, 물음표, 느낌표를 기준으로 문장을 분리합니다.
        // 분리된 문장 뒤에 구분 기호를 다시 붙여줍니다.
        string[] sentences = Regex.Split(fullText, @"(?<=[.?!])\s+");

        foreach (string sentence in sentences)
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                subtitleQueue.Enqueue(sentence.Trim());
            }
        }

        // 큐에 문장이 추가되었고, 현재 자막이 표시되고 있지 않다면 자막 표시를 시작합니다.
        if (!isSubtitleShowing && subtitleQueue.Count > 0)
        {
            StartCoroutine(ProcessSubtitleQueue());
        }
    }

    private IEnumerator ProcessSubtitleQueue()
    {
        // 현재 자막 표시가 시작되었음을 알립니다.
        isSubtitleShowing = true;

        // 큐에 처리할 문장이 남아있는 동안 계속 반복합니다.
        while (subtitleQueue.Count > 0)
        {
            // 큐에서 다음 문장을 꺼냅니다.
            string nextSubtitle = subtitleQueue.Dequeue();

            // 자막 UI에 텍스트를 표시합니다.
            subtitleDisplay.text = nextSubtitle;
            subtitleDisplay.gameObject.SetActive(true);

            // 지정된 시간만큼 자막을 보여줍니다.
            yield return new WaitForSeconds(subtitleDisplayDuration);
        }

        // 모든 자막 표시가 끝나면 UI를 숨기고 상태를 초기화합니다.
        subtitleDisplay.gameObject.SetActive(false);
        subtitleDisplay.text = "";
        isSubtitleShowing = false;
    }

    private void ShowSubtitle(string text)
    {
        // 이전에 실행 중이던 자막 코루틴이 있다면 중지
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
        }
        // 새로운 자막 코루틴 시작
        subtitleCoroutine = StartCoroutine(ShowSubtitleCoroutine(text));
    }

    private IEnumerator ShowSubtitleCoroutine(string text)
    {
        if (subtitleDisplay == null) yield break; // 자막 UI가 없으면 종료

        subtitleDisplay.text = text;
        subtitleDisplay.gameObject.SetActive(true);

        // 지정된 시간만큼 기다립니다.
        yield return new WaitForSeconds(subtitleDisplayDuration);

        subtitleDisplay.gameObject.SetActive(false);
        subtitleDisplay.text = "";
    }
    #endregion
}