using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public enum CommandType { Forward, TurnRight, TurnLeft, Loop }

    [Header("UI 하이라이트 설정")]
    public float highlightScale = 1.2f;

    // --- 리스트 관리 ---
    private List<CommandType> mainCommandList = new List<CommandType>();
    private List<CommandType> loopCommandConfig = new List<CommandType>();

    // --- UI 참조 ---
    private GameObject commandSequencePanel;
    private GameObject loopSequencePanel;
    private GameObject commandSlotPrefab;
    private GameObject successPanel;
    private GameObject loopConfigPopup;
    private TextMeshProUGUI limitText;
    private GameObject inGameUIGroup;

    // --- 아이콘 스프라이트 ---
    [Header("아이콘 스프라이트")]
    public Sprite forwardIcon;
    public Sprite rightIcon;
    public Sprite leftIcon;
    public Sprite loopIcon;
    public Sprite emptySlotSprite;

    // --- 오브젝트 참조 ---
    [Header("오브젝트 연결")]
    public GameObject startBox;
    public GameObject endPoint;

    // --- 플레이어 상태 ---
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isExecuting = false;

    // --- 이동 설정 ---
    [Header("이동 설정")]
    public float moveStep = 1.0f;
    public float turnStep = 90.0f;
    public float moveDuration = 0.5f;
    public float turnDuration = 0.3f;
    public float bumpForce = 0.2f;
    public float bumpDuration = 0.15f;

    // --- 감지 및 제한 설정 ---
    [Header("감지 및 제한 설정")]
    public LayerMask roadLayer;
    public float groundCheckDistance = 2.0f;
    public string obstacleTag = "Obstacle";

    public int loopRepeatCount = 1;
    public int maxLoopConfigLimit = 4;
    public int maxCommandCost = 10;

    // --- 매니저 ---
    private SoundManager soundManager;
    private CameraSwitcher cameraSwitcher;
    private float defaultSlotWidth;

    void Awake()
    {
        // 쇼룸(전시관)에서는 퍼즐용 PlayerController가 작동하여 위치를 강제로 1.33으로 바꾸는 것을 막습니다.
        if (GetComponent<Showroom.MuseumPlayerController>() != null)
        {
            this.enabled = false;
            return;
        }

        // UI 등 초기화에 필요한 값만 미리 계산
        if (commandSlotPrefab != null)
        {
            RectTransform rect = commandSlotPrefab.GetComponent<RectTransform>();
            if (rect != null) defaultSlotWidth = rect.sizeDelta.x;
        }
    }

    IEnumerator Start()
    {
        // 컴포넌트가 꺼져있다면(쇼룸 씬) 여기서 즉시 중단합니다.
        if (!this.enabled) yield break;

        soundManager = FindObjectOfType<SoundManager>();
        cameraSwitcher = FindObjectOfType<CameraSwitcher>();

        // 0.1초 대기하여 MapEditor가 startBox를 넣어줄 시간을 줍니다.
        yield return new WaitForSeconds(0.1f);

        // 만약 MapEditor가 startBox를 안 넣어줬다면(예: 그냥 씬 실행), 스스로 태그를 찾습니다.
        if (startBox == null)
        {
            GameObject[] allStartBoxes = GameObject.FindGameObjectsWithTag("StartBox");
            if (allStartBoxes.Length > 0) 
            {
                // 유효한 StartBox 찾기 (GhostBlock 제외)
                List<GameObject> validBoxes = new List<GameObject>();
                foreach(var box in allStartBoxes)
                {
                    if (box.name.Contains("Ghost") || box.name.Contains("ghost")) continue;
                    validBoxes.Add(box);
                }

                if (validBoxes.Count > 0)
                {
                    startBox = validBoxes[0];
                    Debug.Log($"[PlayerController] Valid StartBox connected: {startBox.name} (ID: {startBox.GetInstanceID()})");
                    
                    // ★ 상세 진단 로그 복구 (다시 확인 필요)
                    Debug.Log($"--- StartBox Detail info ---");
                    Debug.Log($"Name: {startBox.name}");
                    Debug.Log($"Parent: {(startBox.transform.parent ? startBox.transform.parent.name : "None")}");
                    Debug.Log($"Local Rotation (Inspector값): {startBox.transform.localRotation.eulerAngles}");
                    Debug.Log($"World Rotation (실제값): {startBox.transform.rotation.eulerAngles}");
                    Debug.Log($"----------------------------");
                }
                else
                {
                    Debug.LogWarning("[PlayerController] StartBox 태그가 있는 오브젝트는 발견했지만, 모두 GhostBlock(고스트)으로 판단되어 제외되었습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerController] 경고: 'StartBox' 태그를 가진 오브젝트가 씬에 없습니다!");
            }
        }

        // 확정된 startBox를 기준으로 위치/회전 초기화
        ResetPlayerPosition();
    }

    [ContextMenu("Delete Invalid StartBoxes (Rotation ~0)")]
    public void DeleteInvalidStartBoxes()
    {
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("StartBox");
        int deletedCount = 0;
        foreach (var box in boxes)
        {
            // 부동소수점 오차를 고려하여 0도와 1도 차이 이내면 삭제 대상
            if (Quaternion.Angle(box.transform.rotation, Quaternion.identity) < 1.0f)
            {
                Debug.Log($"[Manual-Cleanup] 삭제됨: {box.name} (ID: {box.GetInstanceID()})");
                if (Application.isPlaying) Destroy(box);
                else DestroyImmediate(box);
                deletedCount++;
            }
        }
        Debug.Log($"[Manual-Cleanup] 완료. 총 {deletedCount}개의 잘못된 StartBox를 삭제했습니다.");
    }

    [ContextMenu("Select All StartBoxes")]
    public void SelectAllStartBoxes()
    {
#if UNITY_EDITOR
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("StartBox");
        UnityEditor.Selection.objects = boxes;
        Debug.Log($"[Select-Tool] 총 {boxes.Length}개의 StartBox를 선택했습니다. Hierarchy 창을 확인하세요.");
#endif
    }

    public void InitializeUI(GameObject mainPanel, GameObject loopPanel, GameObject slotPrefab, GameObject successPnl, GameObject loopPopup, TextMeshProUGUI limitTxt, GameObject inGameUI)
    {
        this.commandSequencePanel = mainPanel;
        this.loopSequencePanel = loopPanel;
        this.commandSlotPrefab = slotPrefab;
        this.successPanel = successPnl;
        this.loopConfigPopup = loopPopup;
        this.limitText = limitTxt;
        this.inGameUIGroup = inGameUI;
        // ... (rest of method if needed, but tool replaces contiguous block)

        if (commandSlotPrefab != null)
        {
            RectTransform rect = commandSlotPrefab.GetComponent<RectTransform>();
            if (rect != null) defaultSlotWidth = rect.sizeDelta.x;
        }

        ResetPlayer();
    }

    // ---------------------------------------------------------
    // 버튼 기능
    // ---------------------------------------------------------
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

    private bool IsLoopPopupActive() => loopConfigPopup != null && loopConfigPopup.activeSelf;

    // ---------------------------------------------------------
    // 팝업창 관리
    // ---------------------------------------------------------
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

    // ---------------------------------------------------------
    // 실행 및 리셋
    // ---------------------------------------------------------
    public void ExecuteCommands()
    {
        if (isExecuting) return;
        ResetPlayerPosition(); // 위치 리셋 (중요)
        // cameraSwitcher?.SetSpecificCamera(2); // 자동 전환 제거 요청
        StartCoroutine(ExecuteSequence());
    }

    public void ResetGame()
    {
        ResetPlayer();
        cameraSwitcher?.SetSpecificCamera(0);
    }

    private void ResetPlayer()
    {
        ResetPlayerPosition(); // 여기서 위치를 잡음
        mainCommandList.Clear();

        if (commandSequencePanel != null && commandSlotPrefab != null)
        {
            foreach (Transform child in commandSequencePanel.transform) Destroy(child.gameObject);

            for (int i = 0; i < maxCommandCost; i++)
            {
                GameObject slot = Instantiate(commandSlotPrefab, commandSequencePanel.transform);
                Image img = slot.GetComponent<Image>();
                if (img != null && emptySlotSprite != null)
                {
                    img.sprite = emptySlotSprite;
                    img.color = Color.white;
                }

                RectTransform rect = slot.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
            }
        }

        UpdateIcons(loopSequencePanel, loopCommandConfig);
        UpdateLimitText();
    }

    // ★★★ 여기가 수정된 핵심 함수입니다! ★★★
    private void ResetPlayerPosition()
    {
        StopAllCoroutines();
        isExecuting = false;

        if (startBox != null)
        {
            startPosition = new Vector3(
                startBox.transform.position.x,
                startBox.transform.position.y + 1.33f,
                startBox.transform.position.z
            );
            startRotation = startBox.transform.rotation;
        }
        else
        {
            Debug.LogError("PlayerController: StartBox is not assigned!");
        }

        // 계산된 위치로 이동
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        if (successPanel != null) successPanel.SetActive(false);
        if (inGameUIGroup != null) inGameUIGroup.SetActive(true);
    }

    // ---------------------------------------------------------
    // 실행 로직
    // ---------------------------------------------------------
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
            if (successPanel != null) successPanel.SetActive(true);
            if (inGameUIGroup != null) inGameUIGroup.SetActive(false);

            MapEditor mapEditor = FindObjectOfType<MapEditor>();
            if (mapEditor != null)
            {
                mapEditor.OnLevelCleared();
            }

            StopAllCoroutines();
            isExecuting = false;
        }
    }

    // ---------------------------------------------------------
    // UI 업데이트
    // ---------------------------------------------------------
    private void UpdateIcons(GameObject panel, List<CommandType> list)
    {
        if (panel == null || commandSlotPrefab == null) return;

        if (panel == commandSequencePanel) // 메인 패널
        {
            int cmdIndex = 0;
            for (int i = 0; i < panel.transform.childCount; i++)
            {
                Transform child = panel.transform.GetChild(i);
                Image img = child.GetComponent<Image>();
                RectTransform rect = child.GetComponent<RectTransform>();

                if (img == null || rect == null) continue;

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
                            rect.sizeDelta = new Vector2(defaultSlotWidth * 2f, rect.sizeDelta.y);
                            break;
                    }
                    img.color = Color.white;
                    cmdIndex++;
                }
                else
                {
                    img.sprite = emptySlotSprite;
                    rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                    img.color = Color.white;
                }
            }
        }
        else // 팝업 패널
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
                            if (rect != null) rect.sizeDelta = new Vector2(defaultSlotWidth * 2f, rect.sizeDelta.y);
                            break;
                    }
                    img.color = Color.white;
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



}
