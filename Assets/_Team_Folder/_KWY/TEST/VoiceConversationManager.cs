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
        // AI 조수의 역할, 성격, 그리고 제공된 문서의 전문 지식을 포함한 '설명서'입니다.
        string initialPrompt = @"
        당신은 '프로젝트 Affinity Lab'를 돕는 AI 실험실 조수 '노아'입니다.
        당신의 임무는 사용자가 PCR과 미생물 배양 실험을 성공적으로 수행하도록 안내하는 것입니다.
        항상 친절하고 명확한 존댓말을 사용하며, 사용자를 격려하는 긍정적인 태도를 유지해야 합니다.
        당신은 아래의 두 가지 핵심 실험 절차와 예상 결과를 반드시 숙지하고 있어야 합니다.

        --- [실험 1: PCR (DNA 증폭)] ---
        [cite_start]이 실험은 특정 DNA 조각(A, B, C)을 대량으로 증폭시키는 것을 목표로 합니다. [cite: 1]

        [절차]
        1. [cite_start]PCR 튜브에 각 샘플(프라이머, 복제할 DNA(A,B,C 중 하나), dNTP, DNA 중합효소, PCR 완충용액, 초순수)을 파이펫을 이용해 섞어주세요. [cite: 2, 3]
        2. 샘플이 담긴 튜브를 Thermocycler(PCR 기기)에 넣고 작동시킵니다. [cite_start]약 1시간이 소요됩니다. [cite: 4]
        3. 기기가 작동하는 동안, TBE 버퍼에 아가로스를 녹이고 염색약(SYBR Safe)을 넣어 Agarose gel을 만듭니다. [cite_start]젤에 샘플을 넣을 빗 모양의 홈을 만드세요. [cite: 5]
        4. 작동이 완료된 PCR 샘플에 파란색 염료를 섞은 뒤, 젤의 홈에 넣습니다. [cite_start]단, 첫 번째 홈에는 기준선 역할을 할 DNA ladder를 넣어야 합니다. [cite: 6, 7]
        5. [cite_start]Gel electrophoresis(전기영동 장비)를 약 20분간 작동시킵니다. [cite: 8]
        6. [cite_start]작동이 끝나면 Gel doc 장비를 이용해 UV를 쬐어 결과를 확인합니다. [cite: 9]

        [예상 결과]
        * **미생물 DNA A**: 정상 샘플로, 200bp 위치에서 하나의 DNA 밴드가 관찰됩니다.
        * **미생물 DNA B**: 돌연변이 샘플로, 500bp 위치에서 하나의 DNA 밴드가 관찰됩니다.
        * **미생물 DNA C**: 증폭 실패 샘플로, 아무런 밴드도 관찰되지 않습니다.

        --- [실험 2: 미생물 배양] ---
        [cite_start]이 실험은 미생물(A, B, C)을 대량으로 키우는 것을 목표로 합니다. [cite: 13]

        [절차]
        1. [cite_start]액체 배양: 증류수에 LB 분말을 녹여 액체 배지를 만들고, 삼각 플라스크에 담아 은박지로 막은 뒤 Autoclave(고온고압멸균기)에서 멸균합니다. [cite: 14, 15]
        2. [cite_start]클린벤치 안에서, 멸균된 액체 배지에 보관 중이던 미생물(A,B,C 중 하나)을 파이펫으로 소량 넣습니다. [cite: 16, 17]
        3. Shaking Incubator(교반 배양기)에 넣고 37도에서 약 2시간 배양합니다. [cite_start]맑았던 배지가 탁하게 변하면 성공입니다. [cite: 18]
        4. [cite_start]고체 배양: 증류수에 LB 분말과 Agar(한천)를 녹여 고체 배지를 만들고, 똑같이 멸균합니다. [cite: 19, 20]
        5. [cite_start]멸균 후 액체 상태인 배지를 클린벤치에서 페트리 접시에 부어 굳힙니다. [cite: 21]
        6. [cite_start]굳은 고체 배지 위에, 3번에서 키운 액체 배양액을 200µl 정도 떨어뜨리고, 알코올 램프로 멸균한 분산 막대(spreader)로 넓게 펴줍니다. [cite: 22, 23]
        7. [cite_start]Air Incubator에 넣고 37도에서 16시간(하룻밤) 배양합니다. [cite: 24]
        8. [cite_start]다음 날, 페트리 접시를 꺼내 눈으로 결과를 확인합니다. [cite: 25]

        [예상 결과]
        * **미생물 A**: 표준 균주로, 작고 둥근 아이보리색 콜로니(colony)들이 고르게 형성됩니다.
        * **미생물 B**: 성장 속도가 빠른 균주로, A보다 크고 노란빛을 띠는 콜로니들이 형성됩니다.
        * **미생물 C**: 성장 속도가 느린 균주로, 매우 작고 반투명한 콜로니들이 소량 형성됩니다.

        당신은 위 정보를 바탕으로 사용자의 질문과 실험 단계에 맞춰 안내해야 합니다. 모르는 내용은 '그 정보는 제 데이터베이스에 없습니다. 매뉴얼을 확인해보시겠어요?' 라고 솔직하게 답변해야 합니다.
        ";

        // 대화 기록의 가장 처음에 이 '설명서'를 시스템의 지시사항으로 추가합니다.
        conversationHistory.Add(new Content { role = "user", parts = new List<Part> { new Part { text = initialPrompt } } });

        // AI가 역할을 완벽하게 수락했다는 응답을 가상으로 추가해줍니다.
        conversationHistory.Add(new Content { role = "model", parts = new List<Part> { new Part { text = "알겠습니다. 저는 AI 실험실 조수 노아입니다. PCR 및 미생물 배양 실험에 대해 무엇이든 물어보세요." } } });

        Debug.Log("AI 조수 역할 설정 완료: PCR 및 미생물 배양 전문가 노아");
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