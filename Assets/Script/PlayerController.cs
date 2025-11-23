using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public enum CommandType { Forward, TurnRight, TurnLeft, Loop }

    [Header("설정")]
    public float highlightScale = 1.2f; // 아이콘 확대 배율
    public int maxCommandCost = 10;     // 스테이지 최대 코스트
    public int loopRepeatCount = 1;     // 루프 반복 횟수
    public int maxLoopConfigLimit = 4;  // 루프 설정 최대 개수

    [Header("이동 및 감지")]
    public float moveStep = 1.0f;
    public float turnStep = 90.0f;      // (★ 누락되었던 변수 복구)
    public float moveDuration = 0.5f;
    public float turnDuration = 0.3f;
    public float bumpForce = 0.2f;
    public float bumpDuration = 0.15f;
    public float groundCheckDistance = 2.0f;
    public LayerMask roadLayer;
    public string obstacleTag = "Obstacle";

    [Header("아이콘 리소스")]
    public Sprite forwardIcon;
    public Sprite rightIcon;
    public Sprite leftIcon;
    public Sprite loopIcon;
    public Sprite emptySlotSprite;

    [Header("오브젝트 연결")]
    public GameObject startBox;

    // --- 내부 상태 변수 ---
    private List<CommandType> mainCommandList = new List<CommandType>();
    private List<CommandType> loopCommandConfig = new List<CommandType>();

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isExecuting = false;

    // --- UI 참조 ---
    private GameObject commandSequencePanel;
    private GameObject loopSequencePanel;
    private GameObject commandSlotPrefab;
    private GameObject successPanel;
    private GameObject loopConfigPopup;
    private TextMeshProUGUI limitText;

    // --- 매니저 참조 ---
    private CameraSwitcher cameraSwitcher;

    private float defaultSlotWidth;

    void Start()
    {
        if (startBox == null) return;

        if (commandSlotPrefab != null)
        {
            RectTransform rect = commandSlotPrefab.GetComponent<RectTransform>();
            if (rect != null) defaultSlotWidth = rect.sizeDelta.x;
        }

        // 시작 위치 보정 (높이값 1.33f)
        startPosition = new Vector3(startBox.transform.position.x, startBox.transform.position.y + 1.33f, startBox.transform.position.z);
        startRotation = transform.rotation;

        transform.position = startPosition;
        transform.rotation = startRotation;

        cameraSwitcher = FindObjectOfType<CameraSwitcher>();
    }

    // UI_Auto_Connector에서 호출하여 UI 연결
    public void InitializeUI(GameObject mainPanel, GameObject loopPanel, GameObject slotPrefab, GameObject successPnl, GameObject loopPopup, TextMeshProUGUI limitTxt)
    {
        this.commandSequencePanel = mainPanel;
        this.loopSequencePanel = loopPanel;
        this.commandSlotPrefab = slotPrefab;
        this.successPanel = successPnl;
        this.loopConfigPopup = loopPopup;
        this.limitText = limitTxt;

        ResetPlayer(); // 초기화 실행
    }

    #region 버튼 기능 (통합)

    public void AddCommand_Forward() => TryAddCommand(CommandType.Forward);
    public void AddCommand_TurnRight() => TryAddCommand(CommandType.TurnRight);
    public void AddCommand_TurnLeft() => TryAddCommand(CommandType.TurnLeft);

    public void AddCommand_Loop_ToMain()
    {
        if (isExecuting) return;
        AddToMainList(CommandType.Loop);
    }

    private void TryAddCommand(CommandType type)
    {
        if (isExecuting) return;

        if (IsLoopPopupActive()) AddToLoopConfig(type);
        else AddToMainList(type);
    }

    private void AddToMainList(CommandType type)
    {
        int cost = (type == CommandType.Loop) ? 2 : 1;
        if (GetCurrentCost() + cost > maxCommandCost)
        {
            SoundManager.Instance?.PlayBump();
            return;
        }

        mainCommandList.Add(type);
        UpdateIcons(commandSequencePanel, mainCommandList);
        UpdateLimitText();
        SoundManager.Instance?.PlayCommandClick();
    }

    private void AddToLoopConfig(CommandType type)
    {
        if (loopCommandConfig.Count >= maxLoopConfigLimit)
        {
            SoundManager.Instance?.PlayBump();
            return;
        }

        loopCommandConfig.Add(type);
        UpdateIcons(loopSequencePanel, loopCommandConfig);
        SoundManager.Instance?.PlayCommandClick();
    }

    #endregion

    #region 팝업창 관리

    private bool IsLoopPopupActive() => loopConfigPopup != null && loopConfigPopup.activeSelf;

    public void OpenLoopConfigPopup()
    {
        if (isExecuting || loopConfigPopup == null) return;
        loopConfigPopup.SetActive(true);
        UpdateIcons(loopSequencePanel, loopCommandConfig);
    }

    public void CloseLoopConfigPopup() => loopConfigPopup?.SetActive(false);

    public void ClearLoopConfig()
    {
        if (isExecuting) return;
        loopCommandConfig.Clear();
        UpdateIcons(loopSequencePanel, loopCommandConfig);
        SoundManager.Instance?.PlayResetClick();
    }

    #endregion

    #region 실행 및 리셋

    public void ExecuteCommands()
    {
        if (isExecuting) return;
        ResetPlayerPosition();
        cameraSwitcher?.SetSpecificCamera(2); // 3인칭 카메라
        StartCoroutine(ExecuteSequence());
    }

    public void ResetGame()
    {
        ResetPlayer();
        cameraSwitcher?.SetSpecificCamera(0); // 메인 카메라로 복귀
    }

    // (★ 누락되었던 ResetPlayer 함수 복구)
    // (★ 수정된 함수)
    private void ResetPlayer()
    {
        ResetPlayerPosition();
        mainCommandList.Clear();

        // 1. 메인 패널 초기화: 최대 코스트만큼 빈 슬롯 미리 생성
        if (commandSequencePanel != null && commandSlotPrefab != null)
        {
            // 기존 것 싹 지우고
            foreach (Transform child in commandSequencePanel.transform) Destroy(child.gameObject);

            // 최대 코스트만큼 빈 슬롯 생성
            for (int i = 0; i < maxCommandCost; i++)
            {
                GameObject slot = Instantiate(commandSlotPrefab, commandSequencePanel.transform);
                Image img = slot.GetComponent<Image>();
                if (img != null && emptySlotSprite != null)
                {
                    img.sprite = emptySlotSprite; // 빈 이미지로 설정
                }
            }
        }

        // 루프 팝업 패널은 기존 방식(리스트만큼만 표시) 유지
        UpdateIcons(loopSequencePanel, loopCommandConfig);

        UpdateLimitText();
        cameraSwitcher?.SetSpecificCamera(0);
    }

    private void ResetPlayerPosition()
    {
        StopAllCoroutines();
        isExecuting = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        successPanel?.SetActive(false);
    }

    #endregion

    #region 메인 로직 (코루틴)

    IEnumerator ExecuteSequence()
    {
        isExecuting = true;
        if (!IsGrounded()) { FailSequence(); yield break; }

        int index = 0;
        foreach (CommandType cmd in mainCommandList)
        {
            HighlightIcon(commandSequencePanel, index, true);

            if (cmd == CommandType.Loop)
            {
                for (int i = 0; i < loopRepeatCount; i++)
                {
                    foreach (CommandType subCmd in loopCommandConfig)
                    {
                        yield return StartCoroutine(ProcessMove(subCmd));
                        yield return new WaitForSeconds(0.1f);
                        if (!CheckGameState()) yield break;
                    }
                }
            }
            else
            {
                yield return StartCoroutine(ProcessMove(cmd));
                yield return new WaitForSeconds(0.1f);
                if (!CheckGameState()) yield break;
            }

            HighlightIcon(commandSequencePanel, index, false);
            index++;
        }
        isExecuting = false;
    }

    IEnumerator ProcessMove(CommandType cmd)
    {
        SoundManager.Instance?.PlayStep();
        switch (cmd)
        {
            case CommandType.Forward: yield return StartCoroutine(MoveForward()); break;
            case CommandType.TurnRight: yield return StartCoroutine(Turn(turnStep)); break;
            case CommandType.TurnLeft: yield return StartCoroutine(Turn(-turnStep)); break;
        }
    }

    private bool CheckGameState()
    {
        if (!isExecuting) return false;
        if (!IsGrounded()) { FailSequence(); return false; }
        return true;
    }

    private void FailSequence()
    {
        SoundManager.Instance?.PlayFall();
        ResetPlayerPosition();
    }

    // --- 물리 이동 ---
    IEnumerator MoveForward()
    {
        RaycastHit hit;
        bool hasObstacle = Physics.Raycast(transform.position, transform.forward, out hit, moveStep);

        if (hasObstacle && hit.collider.CompareTag(obstacleTag))
        {
            SoundManager.Instance?.PlayBump();
            Vector3 originalPos = transform.position;
            Vector3 bumpPos = originalPos - transform.forward * bumpForce;

            for (float t = 0; t < bumpDuration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(bumpPos, originalPos, t / bumpDuration);
                yield return null;
            }
            transform.position = originalPos;
            yield break;
        }

        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.forward * moveStep;

        for (float t = 0; t < moveDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, end, t / moveDuration);
            yield return null;
        }
        transform.position = end;
    }

    IEnumerator Turn(float angle)
    {
        Quaternion start = transform.rotation;
        Quaternion end = transform.rotation * Quaternion.Euler(0, angle, 0);

        for (float t = 0; t < turnDuration; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(start, end, t / turnDuration);
            yield return null;
        }
        transform.rotation = end;
    }

    bool IsGrounded() => Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, roadLayer);

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EndPoint") && isExecuting)
        {
            SoundManager.Instance?.PlaySuccess();
            successPanel?.SetActive(true);
            StopAllCoroutines();
            isExecuting = false;
        }
    }

    #endregion

    #region UI 업데이트

    // ---------------------------------------------------------
    // 6. UI 업데이트 (빈 슬롯 기능 + Loop 크기 조절 포함)
    // ---------------------------------------------------------
    private void UpdateIcons(GameObject panel, List<CommandType> list)
    {
        if (panel == null || commandSlotPrefab == null) return;

        // A. 메인 커맨드 패널인 경우 (미리 만들어진 슬롯을 교체하는 방식)
        if (panel == commandSequencePanel)
        {
            int cmdIndex = 0;
            // 패널에 있는 모든 슬롯(자식)을 순회하며 업데이트
            for (int i = 0; i < panel.transform.childCount; i++)
            {
                Transform child = panel.transform.GetChild(i);
                Image img = child.GetComponent<Image>();
                RectTransform rect = child.GetComponent<RectTransform>();

                if (img == null || rect == null) continue;

                // 리스트에 명령이 남아있다면 -> 해당 아이콘으로 교체
                if (cmdIndex < list.Count)
                {
                    CommandType cmd = list[cmdIndex];
                    switch (cmd)
                    {
                        case CommandType.Forward:
                            img.sprite = forwardIcon;
                            rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                            break;
                        case CommandType.TurnRight:
                            img.sprite = rightIcon;
                            rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                            break;
                        case CommandType.TurnLeft:
                            img.sprite = leftIcon;
                            rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                            break;
                        case CommandType.Loop:
                            img.sprite = loopIcon;
                            // Loop는 너비 2배
                            rect.sizeDelta = new Vector2(defaultSlotWidth * 2f, rect.sizeDelta.y);
                            break;
                    }
                    img.color = Color.white;
                    cmdIndex++; // 다음 명령으로 이동
                }
                // 리스트보다 더 뒤쪽 슬롯이라면 -> 빈 슬롯 이미지로 복구
                else
                {
                    img.sprite = emptySlotSprite;
                    rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y); // 너비 원상복구
                    img.color = Color.white;
                }
            }
        }
        // B. 루프 팝업 패널인 경우 (기존 방식 유지: 지우고 새로 생성)
        else
        {
            foreach (Transform child in panel.transform) Destroy(child.gameObject);

            foreach (CommandType cmd in list)
            {
                GameObject slot = Instantiate(commandSlotPrefab, panel.transform);
                Image img = slot.GetComponent<Image>();
                RectTransform rect = slot.GetComponent<RectTransform>();

                if (img != null)
                {
                    switch (cmd)
                    {
                        case CommandType.Forward: img.sprite = forwardIcon; break;
                        case CommandType.TurnRight: img.sprite = rightIcon; break;
                        case CommandType.TurnLeft: img.sprite = leftIcon; break;
                        case CommandType.Loop:
                            img.sprite = loopIcon;
                            if (rect != null) rect.sizeDelta = new Vector2(rect.sizeDelta.x * 2f, rect.sizeDelta.y);
                            break;
                    }
                    img.color = (img.sprite != null) ? Color.white : Color.gray;
                }
            }
        }
    }

    private void HighlightIcon(GameObject panel, int index, bool highlight)
    {
        if (panel == null || index < 0 || index >= panel.transform.childCount) return;
        Transform tr = panel.transform.GetChild(index);
        tr.localScale = highlight ? Vector3.one * highlightScale : Vector3.one;
    }

    private int GetCurrentCost()
    {
        int total = 0;
        foreach (var cmd in mainCommandList) total += (cmd == CommandType.Loop) ? 2 : 1;
        return total;
    }

    private void UpdateLimitText()
    {
        if (limitText == null) return;
        int current = GetCurrentCost();
        limitText.text = $"{current} / {maxCommandCost}";
        limitText.color = (current >= maxCommandCost) ? Color.red : Color.white;
    }

    #endregion
}