using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem; // 새로운 입력 시스템을 사용합니다.
using TMPro; // TextMeshPro UI 컴포넌트를 사용하기 위해 필요합니다.
using Newtonsoft.Json; // 안정적인 JSON 처리를 위한 Newtonsoft.Json 라이브러리입니다.
using Oculus.Voice;


#region Data_Structures
// Gemini API와 통신하기 위한 데이터 구조
[System.Serializable] public class GeminiRequest { public List<Content> contents; }
[System.Serializable] public class Content { public string role; public List<Part> parts; }
[System.Serializable] public class Part { public string text; }
[System.Serializable] public class GeminiResponse { public List<Candidate> candidates; }
[System.Serializable] public class Candidate { public Content content; }

// secrets.json 파일에서 API 키를 읽어오기 위한 구조
//[System.Serializable] public class Secrets { public string apiKey; }
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

    // 비공개 변수들
    private string apiKey;
    private HttpClient httpClient;
    private readonly List<Content> conversationHistory = new List<Content>();
    private bool isLoading = false;
    private CancellationTokenSource cancellationTokenSource;
    private Coroutine subtitleCoroutine;

    #region Unity Lifecycle
    // 스크립트가 처음 활성화될 때 호출됩니다.
    private void Awake()
    {
        // --- API 키 로드 ---
        // StreamingAssets 폴더에서 secrets.json 파일을 읽어 Gemini API 키를 설정합니다.
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

    // Awake 이후, 첫 프레임 업데이트 전에 호출됩니다.
    private void OnEnable()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionReceived);

            appVoiceExperience.VoiceEvents.OnSend.AddListener((_) => Debug.Log("음성 인식 요청 전송됨."));
            appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(() => Debug.Log("음성 인식 종료됨."));
        }
    }

    // 스크립트가 비활성화될 때 호출됩니다.
    private void OnDisable()
    {
        // 메모리 누수를 방지하기 위해 연결했던 이벤트를 해제합니다.
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnTranscriptionReceived);
        }
    }

    // 매 프레임마다 호출됩니다.
    private void Update()
    {
        // 로딩 중이 아닐 때, 키보드의 'T' 키를 누르면 음성 인식을 시작/중지합니다.
        if (!isLoading && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleVoiceRecognition();
        }
    }

    // 오브젝트가 파괴될 때 호출됩니다.
    private void OnDestroy()
    {
        // 앱 종료 시 리소스를 정리합니다.
        httpClient?.Dispose();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// 음성 인식 활성화/비활성화를 토글합니다.
    /// </summary>
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

    /// <summary>
    /// Meta STT로부터 최종 인식 텍스트를 받았을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="transcribedText">음성 인식된 텍스트</param>
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

    /// <summary>
    /// STT로 변환된 텍스트를 Gemini API로 전송하고 답변을 받아 처리합니다.
    /// </summary>
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
                        // 자막 표시 코루틴 시작
                        ShowSubtitle(modelResponseText);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Gemini 오류]: {e.ToString()}");
            ShowSubtitle($"오류가 발생했습니다: {e.Message}");
        }
        finally
        {
            isLoading = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 자막을 표시하는 코루틴을 관리합니다.
    /// </summary>
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

    /// <summary>
    /// 실제로 자막을 일정 시간 동안 표시했다가 사라지게 하는 코루틴입니다.
    /// </summary>
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