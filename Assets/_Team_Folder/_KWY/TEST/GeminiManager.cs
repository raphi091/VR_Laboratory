// Gemini 1.5 Flash API 통신을 위한 최종 안정 버전 스크립트
// 실시간 스트리밍 대신, 전체 응답을 한 번에 받아 처리하여 안정성을 확보합니다.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

[System.Serializable]
public class Secrets
{
    public string apiKey;
}

public class GeminiManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("UI 컴포넌트")]
    [SerializeField] private TMP_Text chatDisplay;
    [SerializeField] private TMP_InputField userInputField;
    [SerializeField] private Button sendButton;

    private string apiKey;
    #endregion

    #region Private Variables
    private HttpClient httpClient;
    private readonly List<Content> conversationHistory = new List<Content>();
    private bool isLoading = false;
    private readonly Queue<string> textUpdateQueue = new Queue<string>();
    private CancellationTokenSource cancellationTokenSource;
    #endregion

    #region Gemini API Data Structures
    [Serializable] public class GeminiRequest { public List<Content> contents; }
    [Serializable] public class Content { public string role; public List<Part> parts; }
    [Serializable] public class Part { public string text; }
    [Serializable] private class GeminiStreamResponse { public List<Candidate> candidates; }
    [Serializable] private class Candidate { public Content content; }
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "secrets.json");

        // 파일이 존재할 경우에만 키를 읽어옵니다.
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

        httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    private void Start()
    {
        sendButton.onClick.AddListener(HandleSubmit);
        userInputField.onSubmit.AddListener((_) => HandleSubmit());
    }

    private void Update()
    {
        lock (textUpdateQueue)
        {
            if (textUpdateQueue.Count > 0)
            {
                // 큐에 있는 모든 텍스트 조각을 한 번에 합쳐서 UI에 추가합니다.
                chatDisplay.text += string.Join("", textUpdateQueue);
                textUpdateQueue.Clear();
            }
        }
    }

    private void OnDestroy()
    {
        httpClient?.Dispose();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
    #endregion

    #region Core Logic
    private void HandleSubmit()
    {
        if (!isLoading && !string.IsNullOrWhiteSpace(userInputField.text))
        {
            _ = SendMessageToGeminiAsync();
        }
    }

    /// <summary>
    /// [최종 안정 버전] Gemini API로 메시지를 전송하고, 전체 응답을 한 번에 받아 처리합니다.
    /// </summary>
    private async Task SendMessageToGeminiAsync()
    {
        isLoading = true;
        SetUIState(false);

        string userMessage = userInputField.text;
        userInputField.text = "";
        AppendMessageToDisplay("You", userMessage);
        conversationHistory.Add(new Content { role = "user", parts = new List<Part> { new Part { text = userMessage } } });

        var requestData = new GeminiRequest { contents = conversationHistory };
        // 스트리밍이 아닌 일반 응답을 받기 위해 엔드포인트에서 'stream'을 제거합니다.
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";
        var jsonContent = JsonConvert.SerializeObject(requestData);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        AppendMessageToDisplay("Gemini", "");
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri) { Content = httpContent })
            {
                using (var response = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token))
                {
                    response.EnsureSuccessStatusCode();

                    // ====================== 핵심 수정 로직 ======================
                    // 스트림을 한 줄씩 읽는 대신, 응답 전체를 하나의 문자열로 읽어옵니다.
                    string fullJsonResponse = await response.Content.ReadAsStringAsync();

                    // 전체 응답을 파싱합니다.
                    var fullResponse = JsonConvert.DeserializeObject<GeminiStreamResponse>(fullJsonResponse);
                    var textChunk = fullResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

                    if (!string.IsNullOrEmpty(textChunk))
                    {
                        // UI 업데이트 큐에 텍스트를 추가합니다.
                        lock (textUpdateQueue)
                        {
                            textUpdateQueue.Enqueue(textChunk);
                        }
                    }
                    // ========================================================
                }
            }

            string fullModelResponse = GetLastModelResponse();
            if (!string.IsNullOrEmpty(fullModelResponse))
            {
                conversationHistory.Add(new Content { role = "model", parts = new List<Part> { new Part { text = fullModelResponse } } });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"오류 발생: {e.ToString()}");
            AppendMessageToDisplay("Error", $"오류가 발생했습니다.\n{e.Message}");
        }
        finally
        {
            isLoading = false;
            SetUIState(true);
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    private void SetUIState(bool interactable)
    {
        sendButton.interactable = interactable;
        userInputField.interactable = interactable;
    }

    private void AppendMessageToDisplay(string speaker, string message)
    {
        if (chatDisplay.text.Length > 0)
        {
            chatDisplay.text += "\n\n";
        }
        chatDisplay.text += $"<b>{speaker}:</b> {message}";
    }

    private string GetLastModelResponse()
    {
        var lastSpeakerPosition = chatDisplay.text.LastIndexOf("<b>Gemini:</b> ");
        if (lastSpeakerPosition != -1)
        {
            return chatDisplay.text.Substring(lastSpeakerPosition + "<b>Gemini:</b> ".Length);
        }
        return string.Empty;
    }
    #endregion
}