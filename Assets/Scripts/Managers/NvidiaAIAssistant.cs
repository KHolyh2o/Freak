using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class NvidiaAIAssistant : MonoBehaviour
{
    public static NvidiaAIAssistant Instance { get; private set; }

    [Header("API Settings")]
    [Tooltip("NVIDIA API 설정")]
    // ⚠️ 경고: 깃허브에 코드를 올릴 때는 절대 여기에 API 키를 직접 적어두지 마세요!
    public string apiKey = "";
    private string apiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";
    public string modelName = "nvidia/llama-3.1-nemotron-70b-instruct";

    private void Awake()
    {
        // 씬이나 인스펙터에 잘못 저장된 옛날 값을 강제로 무시하고 항상 최신 모델을 사용하도록 덮어씁니다.
        modelName = "nvidia/nemotron-3-ultra-550b-a55b";
        
        // 로컬 설정 파일에서 API 키 불러오기 (Assets/Resources/Config/API_Key.txt)
        TextAsset keyFile = Resources.Load<TextAsset>("Config/API_Key");
        if (keyFile != null && !string.IsNullOrWhiteSpace(keyFile.text))
        {
            // 메모장에 적힌 키 양옆의 공백이나 줄바꿈을 제거하고 적용
            apiKey = keyFile.text.Trim();
        }
        else if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("[NvidiaAIAssistant] API 키를 찾을 수 없습니다. Assets/Resources/Config/API_Key.txt 파일에 키를 입력해 주세요.");
        }
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestAIHelp(string missingSkill)
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.isAISupportEnabled)
        {
            Debug.Log("[NvidiaAIAssistant] AI 서포트가 설정에서 꺼져 있으므로 도움을 요청하지 않습니다.");
            return;
        }

        string prompt = "";
        
        switch(missingSkill)
        {
            case "Spatial":
                prompt = "플레이어가 방향을 자꾸 헷갈려서 맵 밖으로 떨어졌어. 좌/우회전 방향 감각을 연습할 수 있게 짧은 격려와 연습 맵 제안을 존댓말로 1~2줄로 해줘.";
                break;
            case "Function":
                prompt = "플레이어가 코스트를 초과해서 실패했는데, 함수 기능을 전혀 사용하지 않고 있어. 반복되는 패턴을 찾아 함수로 묶어보자고 짧게 격려하며 연습 맵을 제안해줘. 존댓말 1~2줄.";
                break;
            case "If":
                prompt = "플레이어가 조건문(if)을 사용하지 않고 실패하고 있어. 상황에 따라 다르게 행동하는 조건문의 필요성을 말하며 짧게 격려하고 연습 맵을 제안해줘. 존댓말 1~2줄.";
                break;
            case "While":
                prompt = "플레이어가 반복문(while)을 사용하지 않아서 코스트를 낭비하고 있어. 반복문을 쓰면 편하다고 짧게 격려하며 연습 맵을 제안해줘. 존댓말 1~2줄.";
                break;
            case "ObstacleAvoidance":
                prompt = "플레이어가 장애물을 피하지 못하고 있어. 장애물을 피하는 논리를 연습해보자고 짧게 격려하고 연습 맵을 제안해줘. 존댓말 1~2줄.";
                break;
            default:
                prompt = "플레이어가 여러 번 실패해서 좌절하고 있어. 짧게 격려하고 기본기 연습 맵을 제안해줘. 존댓말 1~2줄.";
                break;
        }

        StartCoroutine(SendRequest(prompt, missingSkill));
    }

    private IEnumerator SendRequest(string prompt, string missingSkill)
    {
        // UI 표시: AI가 생각 중...
        if (AIPopupUI.Instance != null)
        {
            AIPopupUI.Instance.ShowLoading();
        }

        string jsonData = $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{
                    ""role"": ""system"",
                    ""content"": ""너는 어린이 코딩 교육 게임의 친절한 AI 어시스턴트야. 항상 존댓말을 쓰고, 최대한 짧고 다정하게 말해. 이모티콘이나 특수 기호(*, #, ~ 등)는 절대 쓰지 말고 오직 한글, 영어, 숫자, 온점, 쉼표, 물음표, 느낌표만 써.""
                }},
                {{
                    ""role"": ""user"",
                    ""content"": ""{prompt}""
                }}
            ],
            ""max_tokens"": 150,
            ""temperature"": 0.7
        }}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[NvidiaAIAssistant] Error: {request.error}\n{request.downloadHandler.text}");
            if (AIPopupUI.Instance != null)
            {
                AIPopupUI.Instance.ShowMessage("앗, 통신에 문제가 생겼어요. 다시 시도해 볼까요?", missingSkill, true);
            }
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            string message = ParseMessageFromJson(jsonResponse);
            bool isParsingError = message == "AI 응답을 해석할 수 없습니다.";

            if (AIPopupUI.Instance != null)
            {
                AIPopupUI.Instance.ShowMessage(message, missingSkill, isParsingError);
            }
            else
            {
                Debug.Log($"[AI] {message}");
                // 임시로 바로 생성 및 씬 이동
                PracticeMapGenerator.Instance?.GenerateAndSavePracticeMap(missingSkill);
            }
        }
    }

    // 간단한 정규식으로 content 내용만 파싱 (Newtonsoft Json 없이)
    private string ParseMessageFromJson(string json)
    {
        Match match = Regex.Match(json, @"""content"":\s*""(.*?)""");
        if (match.Success)
        {
            // 이스케이프된 문자열 처리 (\n, \", 등)
            string text = match.Groups[1].Value;
            text = text.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\r", "").Replace("\r", "");
            
            // 이모지(Surrogate 쌍) 및 흔히 깨지는 특수기호 강제 제거
            text = Regex.Replace(text, @"[\uD800-\uDFFF]", ""); // 이모지 제거
            return text;
        }
        return "AI 응답을 해석할 수 없습니다.";
    }
}
