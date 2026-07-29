using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FailReason
{
    None,
    OutOfBounds,
    NotClearedYet
}

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    [Header("AI State")]
    public int attemptCount = 0;
    public List<string> currentStageTags = new List<string>();
    public int triggerThreshold = 3;

    // 참이면 AI 팝업이 이미 떠있거나 이미 연습맵을 제안한 상태
    private bool hasTriggeredAI = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬이 넘어가도 AIManager는 유지되도록 할 수도 있으나, 
            // 스테이지마다 초기화되는게 낫다면 유지하지 않아도 됩니다.
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetStageData()
    {
        attemptCount = 0;
        currentStageTags.Clear();
        hasTriggeredAI = false;
        Debug.Log("[AIManager] 스테이지 데이터가 리셋되었습니다.");
    }

    public void SetStageTags(string tagString)
    {
        currentStageTags.Clear();
        if (string.IsNullOrEmpty(tagString)) return;

        string[] tags = tagString.Split('|');
        foreach (string t in tags)
        {
            currentStageTags.Add(t.Trim());
        }
        Debug.Log($"[AIManager] 현재 스테이지 요구 태그 설정됨: {tagString}");
    }

    public void ReportOutOfBounds()
    {
        if (hasTriggeredAI) return;
        
        attemptCount++;
        Debug.Log($"[AIManager] 추락! (시도 횟수: {attemptCount})");
        CheckTriggerCondition(FailReason.OutOfBounds, null);
    }

    public void ReportAttempt(List<PlayerController.CommandBlock> commands)
    {
        if (hasTriggeredAI) return;

        attemptCount++;
        Debug.Log($"[AIManager] 실행 완료되었으나 미클리어. (시도 횟수: {attemptCount})");
        CheckTriggerCondition(FailReason.NotClearedYet, commands);
    }

    private void CheckTriggerCondition(FailReason reason, List<PlayerController.CommandBlock> commands)
    {
        if (attemptCount >= triggerThreshold)
        {
            hasTriggeredAI = true;
            AnalyzeAndTriggerAI(reason, commands);
        }
    }

    private void AnalyzeAndTriggerAI(FailReason reason, List<PlayerController.CommandBlock> commands)
    {
        string missingSkill = "Sequence"; // 기본값

        if (reason == FailReason.OutOfBounds)
        {
            missingSkill = "Spatial";
        }
        else
        {
            // 커맨드 분석
            bool usedFunction = false;
            bool usedIf = false;
            bool usedWhile = false;

            if (commands != null)
            {
                foreach (var cmd in commands)
                {
                    if (cmd.type == PlayerController.CommandType.CallFunction) usedFunction = true;
                    if (cmd.type == PlayerController.CommandType.If) usedIf = true;
                    if (cmd.type == PlayerController.CommandType.While) usedWhile = true;
                }
            }

            // 스테이지 요구사항과 비교
            if (currentStageTags.Contains("Function") && !usedFunction)
            {
                missingSkill = "Function";
            }
            else if (currentStageTags.Contains("If") && !usedIf)
            {
                missingSkill = "If";
            }
            else if (currentStageTags.Contains("While") && !usedWhile)
            {
                missingSkill = "While";
            }
            else if (currentStageTags.Contains("ObstacleAvoidance"))
            {
                missingSkill = "ObstacleAvoidance";
            }
        }

        Debug.Log($"[AIManager] AI 개입 조건 달성! 부족한 스킬 분석 결과: {missingSkill}");
        
        // NvidiaAIAssistant 호출
        if (NvidiaAIAssistant.Instance != null)
        {
            NvidiaAIAssistant.Instance.RequestAIHelp(missingSkill);
        }
        else
        {
            Debug.LogError("[AIManager] NvidiaAIAssistant 인스턴스를 찾을 수 없습니다.");
        }
    }
}
