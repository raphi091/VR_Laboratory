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
using Newtonsoft.Json;
using Oculus.Voice;

#region Data_Structures_VCM
[System.Serializable] 
public class GeminiRequest_VCM 
{
    public List<Content_VCM> contents; 
}

[System.Serializable] 
public class Content_VCM
{ 
    public string role; 
    public List<Part_VCM> parts; 
}

[System.Serializable] 
public class Part_VCM 
{ 
    public string text;
}

[System.Serializable] 
public class GeminiResponse_VCM 
{
    public List<Candidate_VCM> candidates; 
}

[System.Serializable] 
public class Candidate_VCM
{ 
    public Content_VCM content;
}

[System.Serializable]
public class Secrets_VCM 
{
    public string apiKey; 
}
#endregion

public class VoiceConversationManager_G : MonoBehaviour
{
    public enum ListeningMode
    {
        None,
        ExperimentChoice,
        TaskOrQuestion
    }

    [Header("실험 데이터 참조")]
    [Tooltip("키워드 참조를 위해 PCR 실험 데이터를 연결합니다.")]
    [SerializeField] private ExperimentData_G pcrExperiment;
    [Tooltip("키워드 참조를 위해 배양 실험 데이터를 연결합니다.")]
    [SerializeField] private ExperimentData_G cultureExperiment;

    public event Action OnProcessingStarted;
    public event Action<Queue<string>, string> OnResponseReceived;
    public event Action<ExperimentData_G> OnExperimentChosen;
    public event Action OnTaskCompleted;
    public event Action OnFreeQuestionAsked;
    public event Action OnChoiceNotUnderstood;

    private AppVoiceExperience appVoiceExperience;
    private string apiKey;
    private HttpClient httpClient;
    private readonly List<Content_VCM> conversationHistory = new List<Content_VCM>();
    private bool isConversationLoading = false;
    private CancellationTokenSource cancellationTokenSource;
    private ListeningMode currentMode = ListeningMode.None;
    private List<string> currentCompletionKeywords;

    #region Unity Lifecycle
    private void Awake()
    {
        if (!TryGetComponent(out appVoiceExperience))
            Debug.LogWarning("VoiceConversationManager_G ] AppVoiceExperience 없음");

        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "secrets.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            Secrets_VCM secrets = JsonConvert.DeserializeObject<Secrets_VCM>(json);
            apiKey = secrets.apiKey;
        }
        else Debug.LogError("secrets.json 파일을 찾을 수 없습니다!");

        httpClient = new HttpClient { Timeout = System.TimeSpan.FromMinutes(5) };
        SetupInitialPrompt();
    }

    private void OnEnable()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionReceived);
        }
    }

    private void OnDisable()
    {
        if (appVoiceExperience != null)
        {
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnTranscriptionReceived);
        }
    }

    private void OnDestroy()
    {
        httpClient?.Dispose();
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    private void SetupInitialPrompt()
    {
        string initialPrompt = @"
        당신은 '프로젝트 Affinity Lab'를 돕는 AI 실험실 조수 '노아'입니다.
        당신의 임무는 사용자가 PCR과 미생물 배양 실험을 성공적으로 수행하도록 안내하는 것입니다.
        항상 친절하고 명확한 존댓말을 사용하며, 사용자를 격려하는 긍정적인 태도를 유지해야 합니다.
        당신은 아래의 두 가지 핵심 실험 절차와 예상 결과를 반드시 숙지하고 있어야 합니다.

        [매우 중요한 대화 규칙]
        1.  **간결한 답변**: 당신의 모든 답변은 항상 한두 문장으로 짧고 간결해야 합니다. 절대 길게 설명하지 마세요.
        2.  **단계별 안내**: 실험 절차는 사용자가 '다음 단계 알려줘' 또는 비슷한 요청을 할 때만, 해당하는 다음 단계 '하나만' 설명해야 합니다. 절대로 먼저 전체 절차를 읊어주지 마세요.
        3.  **일반적인 질문**: 사용자가 'PCR이 뭐야?'처럼 실험 절차가 아닌 일반적인 질문을 하면, 'DNA를 증폭시키는 기술입니다. 실험을 시작해볼까요?' 와 같이 핵심 개념만 짧게 답하고 대화를 유도하세요.
        4.  **결과에 대한 침묵**: 당신은 실험 결과를 미리 알지 못합니다. 절대로 결과에 대해 먼저 언급하거나 암시하지 마세요. 결과는 실험이 모두 끝난 후에만 이야기할 수 있습니다.
        
        [핵심 지식: 실험 절차]
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

        당신은 위 지식과 규칙을 바탕으로 사용자의 질문과 실험 단계에 맞춰 안내해야 합니다.
        ";

        conversationHistory.Add(new Content_VCM { role = "user", parts = new List<Part_VCM> { new Part_VCM { text = initialPrompt } } });
        conversationHistory.Add(new Content_VCM { role = "model", parts = new List<Part_VCM> { new Part_VCM { text = "알겠습니다. 저는 AI 실험실 조수 노아입니다. PCR 및 미생물 배양 실험에 대해 무엇이든 물어보세요." } } });
    }
    #endregion

    #region Public Control Methods
    public void StartListeningForChoice()
    {
        currentMode = ListeningMode.ExperimentChoice;
        ActivateVoiceSDK();
    }

    public void StartListeningForTask(List<string> completionKeywords)
    {
        currentCompletionKeywords = completionKeywords;
        currentMode = ListeningMode.TaskOrQuestion;
        ActivateVoiceSDK();
    }

    public void HandlePlayerInteraction()
    {
    }

    private void ActivateVoiceSDK()
    {
        if (!appVoiceExperience.Active && !isConversationLoading)
        {
            appVoiceExperience.Activate();
            Debug.Log($"[VoiceManager] 듣기 시작... (모드: {currentMode})");
        }
    }
    #endregion

    #region Core Conversation Logic
    private void OnTranscriptionReceived(string transcribedText)
    {
        if (isConversationLoading || string.IsNullOrWhiteSpace(transcribedText)) return;

        Debug.Log($"[STT 결과]: {transcribedText}");

        switch (currentMode)
        {
            case ListeningMode.ExperimentChoice:
                ProcessExperimentChoice(transcribedText);
                break;
            case ListeningMode.TaskOrQuestion:
                ProcessTaskOrQuestion(transcribedText);
                break;
        }
        currentMode = ListeningMode.None;
    }

    private void ProcessExperimentChoice(string text)
    {
        string lowerText = text.ToLower();

        string[] pcrKeywords = { "pcr", "피씨알", "첫 번째", "1번" , "일" , "일번" };
        string[] cultureKeywords = { "배양", "두 번째", "2번" , "이", "이번" };

        if (pcrKeywords.Any(keyword => lowerText.Contains(keyword)))
        {
            OnExperimentChosen?.Invoke(pcrExperiment);
        }
        else if (cultureKeywords.Any(keyword => lowerText.Contains(keyword)))
        {
            OnExperimentChosen?.Invoke(cultureExperiment);
        }
        else
        {
            OnChoiceNotUnderstood?.Invoke();
        }
    }

    private void ProcessTaskOrQuestion(string text)
    {
        bool isTaskCompleted = false;
        foreach (string keyword in currentCompletionKeywords)
        {
            if (text.Contains(keyword))
            {
                isTaskCompleted = true;
                break;
            }
        }

        if (isTaskCompleted)
        {
            OnTaskCompleted?.Invoke();
        }
        else
        {
            OnFreeQuestionAsked?.Invoke();
            OnProcessingStarted?.Invoke();
            _ = SendMessageToGeminiAsync(text);
        }
    }

    private async Task SendMessageToGeminiAsync(string userMessage)
    {
        isConversationLoading = true;
        conversationHistory.Add(new Content_VCM { role = "user", parts = new List<Part_VCM> { new Part_VCM { text = userMessage } } });

        var requestData = new GeminiRequest_VCM { contents = conversationHistory };
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";
        var jsonContent = JsonConvert.SerializeObject(requestData);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri) { Content = httpContent })
            {
                using (var response = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token))
                {
                    response.EnsureSuccessStatusCode();
                    string fullJsonResponse = await response.Content.ReadAsStringAsync();
                    var fullResponse = JsonConvert.DeserializeObject<GeminiResponse_VCM>(fullJsonResponse);
                    var modelResponseText = fullResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

                    if (!string.IsNullOrEmpty(modelResponseText))
                    {
                        Debug.Log($"[Gemini 답변]: {modelResponseText}");
                        conversationHistory.Add(new Content_VCM { role = "model", parts = new List<Part_VCM> { new Part_VCM { text = modelResponseText } } });

                        string[] sentences = Regex.Split(modelResponseText, @"(?<=[.?!])\s+");
                        Queue<string> sentenceQueue = new Queue<string>(sentences.Where(s => !string.IsNullOrWhiteSpace(s)));
                        OnResponseReceived?.Invoke(sentenceQueue, modelResponseText);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Gemini 오류]: {e.ToString()}");
            Queue<string> errorQueue = new Queue<string>();
            errorQueue.Enqueue($"오류가 발생했습니다: {e.Message}");
            OnResponseReceived?.Invoke(errorQueue, "오류");
        }
        finally
        {
            isConversationLoading = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
    #endregion
}