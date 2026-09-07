using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public enum CommandType { Forward, TurnRight, TurnLeft, CallFunction, If, While }
    public enum ObstacleType { None, Tree, Box, Rock, Cliff }

    [System.Serializable]
    public class CommandBlock
    {
        public CommandType type;
        public int functionIndex; 
        public ObstacleType conditionObstacle;
        public bool conditionExpectedState = true; 
        public int innerCommandCount = 0; 

        public CommandBlock(CommandType t) { type = t; }
    }

    [Header("UI 하이라이트 설정")]
    public float highlightScale = 1.2f;

    // --- 리스트 관리 ---
    private List<CommandBlock> mainCommandList = new List<CommandBlock>();
    
    public int maxFunctionCount = 3;
    private List<List<CommandBlock>> functionLists = new List<List<CommandBlock>>();

    // --- UI 참조 ---
    [Header("커맨드 패널 UI")]
    public GameObject commandSequencePanel;
    public GameObject[] functionPanels; // F1, F2, F3 패널
    
    // --- UI 연동 ---
    private GameObject commandSlotPrefab;
    private GameObject successPanel;
    private TextMeshProUGUI limitText;
    private GameObject inGameUIGroup;
    private GameObject limitBox;

    // --- 아이콘 스프라이트 ---
    [Header("아이콘 스프라이트")]
    public Sprite forwardIcon;
    public Sprite rightIcon;
    public Sprite leftIcon;
    public Sprite loopIcon; // 기본 함수 아이콘 (이전 버전 호환용)
    public Sprite ifIcon;
    public Sprite whileIcon;
    public Sprite emptySlotSprite; // 기본 빈 슬롯 (이전 버전 호환용)

    [Header("함수 아이콘 (F1, F2, F3)")]
    public Sprite[] functionCallIcons; 

    [Header("빈 슬롯 배경 (F1, F2, F3)")]
    public Sprite[] emptySlotSprites;

    [Header("조건 토글 아이콘 (장애물)")]
    public Sprite obsTreeIcon;
    public Sprite obsBoxIcon;
    public Sprite obsRockIcon;
    public Sprite obsCliffIcon;

    [Header("조건 토글 아이콘 (O/X 상태)")]
    public Sprite stateTrueIcon;
    public Sprite stateFalseIcon;

    // --- 오브젝트 참조 ---
    [Header("오브젝트 연결")]
    public GameObject startBox;
    public GameObject endPoint;

    // --- 플레이어 상태 ---
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isExecuting = false;
    public bool IsExecuting() => isExecuting;

    // --- Scope 수정 상태 ---
    private bool isEditingScope = false;
    private int editingScopeStartIndex = -1;
    private List<CommandBlock> editingScopeList = null;
    private GameObject editingScopePanel = null;

    // --- 편집 및 다중 창 상태 ---
    public int activeListIndex = -1; // -1: 메인, 0: F1, 1: F2, 2: F3
    public int insertIndex = -1;

    // --- 이동 설정 ---
    [Header("이동 설정")]
    public float moveStep = 1.0f;
    public float turnStep = 90.0f;
    public float moveDuration = 0.5f;
    public float turnDuration = 0.3f;
    public float bumpForce = 0.2f;
    public float bumpDuration = 0.15f;

    [Header("패널 슬라이드 애니메이션")]
    public float panelSlideOffset = 80f;
    public float panelSlideSpeed = 10f;
    private bool isPanelsUp = true;
    private System.Collections.Generic.Dictionary<GameObject, Vector2> panelOriginalPos = new System.Collections.Generic.Dictionary<GameObject, Vector2>();

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

        for (int i = 0; i < maxFunctionCount; i++)
        {
            functionLists.Add(new List<CommandBlock>());
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

    private GameObject GetPanelRoot(GameObject panelObj)
    {
        if (panelObj == null) return null;
        // Content를 할당했든 최상위 패널을 할당했든, ScrollRect가 있는 오브젝트가 진짜 패널입니다.
        UnityEngine.UI.ScrollRect scroll = panelObj.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scroll != null) return scroll.gameObject;
        return panelObj;
    }

    public void InitializeUI(GameObject mainPanel, GameObject[] functionPnls, GameObject slotPrefab, GameObject successPnl, TextMeshProUGUI limitTxt, GameObject inGameUI, GameObject limitBox = null)
    {
        if (this.commandSequencePanel == null) this.commandSequencePanel = mainPanel;
        if (this.functionPanels == null || this.functionPanels.Length == 0) this.functionPanels = functionPnls;
        
        this.commandSlotPrefab = slotPrefab;
        this.successPanel = successPnl;
        this.limitText = limitTxt;
        this.inGameUIGroup = inGameUI;
        this.limitBox = limitBox;

        if (limitText != null)
        {
            RectTransform rt = limitText.GetComponent<RectTransform>();
            if (rt != null) panelOriginalPos[limitText.gameObject] = rt.anchoredPosition;
        }

        if (limitBox != null)
        {
            RectTransform rt = limitBox.GetComponent<RectTransform>();
            if (rt != null) panelOriginalPos[limitBox] = rt.anchoredPosition;
        }

        if (commandSequencePanel != null)
        {
            GameObject root = GetPanelRoot(commandSequencePanel);
            RectTransform rt = root.GetComponent<RectTransform>();
            if (rt != null) panelOriginalPos[root] = rt.anchoredPosition;
        }
        
        if (functionPanels != null)
        {
            foreach (var p in functionPanels)
            {
                if (p != null)
                {
                    GameObject root = GetPanelRoot(p);
                    RectTransform rt = root.GetComponent<RectTransform>();
                    if (rt != null) panelOriginalPos[root] = rt.anchoredPosition;
                }
            }
        }
        
        isPanelsUp = true;
        SnapPanelsToTarget();

        if (commandSlotPrefab != null)
        {
            RectTransform rect = commandSlotPrefab.GetComponent<RectTransform>();
            if (rect != null) defaultSlotWidth = rect.sizeDelta.x;
        }

        ResetPlayer();
    }

    private void SnapPanelsToTarget()
    {
        float offset = isPanelsUp ? panelSlideOffset : 0f;
        foreach (var kvp in panelOriginalPos)
        {
            if (kvp.Key == null) continue;
            RectTransform rt = kvp.Key.GetComponent<RectTransform>();
            if (rt == null) continue;
            
            Vector2 targetPos = kvp.Value;
            if (isPanelsUp && (kvp.Key == limitBox || (limitText != null && kvp.Key == limitText.gameObject)))
            {
                targetPos = new Vector2(0f, kvp.Value.y + 40f);
            }
            else
            {
                targetPos.y += offset;
            }
            
            rt.anchoredPosition = targetPos;
        }
    }

    private void Update()
    {
        UpdatePanelSlide();
    }

    private void UpdatePanelSlide()
    {
        if (panelOriginalPos.Count == 0) return;
        float targetOffset = isPanelsUp ? panelSlideOffset : 0f;

        foreach (var kvp in panelOriginalPos)
        {
            if (kvp.Key == null) continue;
            RectTransform rt = kvp.Key.GetComponent<RectTransform>();
            if (rt == null) continue;
            
            Vector2 pos = rt.anchoredPosition;
            Vector2 targetPos = kvp.Value;
            
            if (isPanelsUp && (kvp.Key == limitBox || (limitText != null && kvp.Key == limitText.gameObject)))
            {
                targetPos = new Vector2(0f, kvp.Value.y + 40f);
            }
            else
            {
                targetPos.y += targetOffset;
            }
            
            pos = Vector2.Lerp(pos, targetPos, Time.deltaTime * panelSlideSpeed);
            rt.anchoredPosition = pos;
        }
    }

    // ---------------------------------------------------------
    // 버튼 기능 (커맨드 추가)
    // ---------------------------------------------------------
    public void AddCommand_Forward() => TryAddCommand(new CommandBlock(CommandType.Forward));
    public void AddCommand_TurnRight() => TryAddCommand(new CommandBlock(CommandType.TurnRight));
    public void AddCommand_TurnLeft() => TryAddCommand(new CommandBlock(CommandType.TurnLeft));

    public void AddCommand_If_Tree() => TryAddCommand(new CommandBlock(CommandType.If) { conditionObstacle = ObstacleType.Tree });
    public void AddCommand_While_Tree() => TryAddCommand(new CommandBlock(CommandType.While) { conditionObstacle = ObstacleType.Tree });

    public void AddCommand_CallFunction(int funcIndex)
    {
        if (isExecuting) return;
        CommandBlock callFunc = new CommandBlock(CommandType.CallFunction);
        callFunc.functionIndex = funcIndex;
        TryAddCommand(callFunc);
    }

    private void TryAddCommand(CommandBlock block)
    {
        isPanelsUp = false;
        if (isExecuting) return;

        List<CommandBlock> targetList = GetActiveList();
        
        // 모든 패널에 대해 통합된 코스트 제한 검사
        if (GetCurrentCost() + 1 > maxCommandCost)
        {
            SoundManager.Instance?.PlayBump();
            return;
        }

        if (insertIndex >= 0 && insertIndex <= targetList.Count)
        {
            targetList.Insert(insertIndex, block);
            insertIndex++; // 연속 삽입을 위해 인덱스 1 증가
        }
        else
        {
            targetList.Add(block);
            insertIndex = -1;
        }

        SoundManager.Instance?.PlayCommandClick();
        RefreshAllPanels();
    }

    public List<CommandBlock> GetActiveList()
    {
        if (activeListIndex >= 0 && activeListIndex < functionLists.Count)
            return functionLists[activeListIndex];
        return mainCommandList;
    }

    public void SetActivePanel(int panelIndex)
    {
        if (activeListIndex == panelIndex)
        {
            // 이미 활성화된 패널의 탭을 다시 누른 경우
            isPanelsUp = !isPanelsUp;
        }
        else
        {
            // 다른 탭을 누른 경우 내림
            isPanelsUp = false;
        }

        activeListIndex = panelIndex;
        insertIndex = -1; // 패널이 바뀌면 삽입 지점 초기화
        RefreshAllPanels();
    }

    // ---------------------------------------------------------
    // 팝업창 관리
    // ---------------------------------------------------------
    // ---------------------------------------------------------
    // 팝업창 관리 (레거시 코드, 더 이상 사용되지 않음)
    // ---------------------------------------------------------
    public void OpenLoopConfigPopup() { }
    public void CloseLoopConfigPopup() { }

    public void ClearLoopConfig()
    {
        if (isExecuting) return;
        List<CommandBlock> targetList = GetActiveList();
        targetList.Clear();
        RefreshAllPanels();
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
        // 카메라 시점은 유저가 자유롭게 돌려보던 상태를 유지하도록 리셋하지 않습니다.
        // cameraSwitcher?.SetSpecificCamera(0);
    }

    private void ResetPlayer()
    {
        ResetPlayerPosition(); // 여기서 위치를 잡음
        mainCommandList.Clear();
        foreach (var list in functionLists) list.Clear();

        if (commandSequencePanel != null && commandSlotPrefab != null)
        {
            PopulateSlots(commandSequencePanel, maxCommandCost);
        }

        if (functionPanels != null && commandSlotPrefab != null)
        {
            foreach (GameObject fPanel in functionPanels)
            {
                if (fPanel != null) PopulateSlots(fPanel, maxCommandCost);
            }
        }

        RefreshAllPanels();
        UpdateLimitText();
    }

    private Transform GetPanelContent(GameObject panel)
    {
        Transform viewport = panel.transform.Find("Viewport");
        if (viewport != null)
        {
            Transform content = viewport.Find("Content");
            if (content != null) return content;
        }
        return panel.transform;
    }

    private RectTransform GetDropZoneRect(GameObject panel)
    {
        if (panel == null) return null;
        Transform viewport = panel.transform.Find("Viewport");
        if (viewport != null) return viewport as RectTransform;
        
        UnityEngine.UI.Image bg = panel.GetComponentInChildren<UnityEngine.UI.Image>();
        if (bg != null) return bg.rectTransform;
        
        return panel.GetComponent<RectTransform>();
    }

    private void PopulateSlots(GameObject panel, int maxCount)
    {
        Transform targetContent = GetPanelContent(panel);

        foreach (Transform child in targetContent) Destroy(child.gameObject);

        // 유저가 측정한 기준 좌표
        float panelLocalX = -289.2f;
        float panelLocalY = 18.4f;
        float intervalX = 64.5f;

        // 기준이 되는 Panel (Viewport의 부모)을 찾습니다.
        Transform referencePanel = targetContent;
        if (targetContent.parent != null && targetContent.parent.name == "Viewport")
        {
            referencePanel = targetContent.parent.parent;
        }

        // 실제 패널 기준의 좌표를 월드 좌표로 바꾼 뒤, Content 내부의 로컬 좌표로 다시 변환합니다.
        Vector3 worldPos = referencePanel.TransformPoint(new Vector3(panelLocalX, panelLocalY, 0));
        Vector3 contentLocalPos = targetContent.InverseTransformPoint(worldPos);

        for (int i = 0; i < maxCount; i++)
        {
            GameObject slot = Instantiate(commandSlotPrefab, targetContent);
            Image img = slot.GetComponent<Image>();
            if (img != null && emptySlotSprite != null)
            {
                img.sprite = emptySlotSprite;
                img.color = Color.white;
            }

            RectTransform rect = slot.GetComponent<RectTransform>();
            if (rect != null)
            {
                // UI 앵커에 구애받지 않도록 localPosition을 직접 세팅합니다.
                rect.localPosition = new Vector3(contentLocalPos.x + (i * intervalX), contentLocalPos.y, 0);
                rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
            }
        }
        
        // 스크롤이 작동하도록 Content의 가로 길이를 명령 블록 개수에 맞춰 늘려줍니다.
        RectTransform contentRect = targetContent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            float totalWidth = Mathf.Abs(contentLocalPos.x) + (maxCount * intervalX) + 50f;
            contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
        }
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
        if (commandSequencePanel != null) commandSequencePanel.SetActive(true);
        if (limitText != null) limitText.gameObject.SetActive(true);
        if (limitBox != null) limitBox.SetActive(true);
        if (functionPanels != null)
        {
            foreach (var p in functionPanels)
            {
                if (p != null) p.SetActive(true);
            }
        }
    }

    // ---------------------------------------------------------
    // 실행 로직
    // ---------------------------------------------------------
    IEnumerator ExecuteSequence()
    {
        isExecuting = true;
        if (!IsGrounded()) { FailSequence(); yield break; }

        yield return StartCoroutine(ExecuteBlockList(mainCommandList, commandSequencePanel));

        isExecuting = false;

        // AI Assistant: Report attempt if finished without success
        if (successPanel != null && !successPanel.activeSelf)
        {
            if (AIManager.Instance != null)
            {
                AIManager.Instance.ReportAttempt(mainCommandList);
            }
        }
    }

    IEnumerator ExecuteBlockList(List<CommandBlock> blockList, GameObject panel)
    {
        for (int i = 0; i < blockList.Count; i++)
        {
            if (!isExecuting) yield break;

            CommandBlock block = blockList[i];
            
            if (panel != null) HighlightIcon(panel, i, true);

            switch (block.type)
            {
                case CommandType.Forward:
                case CommandType.TurnRight:
                case CommandType.TurnLeft:
                    yield return StartCoroutine(ProcessMove(block.type));
                    yield return new WaitForSeconds(0.1f);
                    break;
                
                case CommandType.CallFunction:
                    if (block.functionIndex >= 0 && block.functionIndex < functionLists.Count)
                    {
                        GameObject funcPanel = (functionPanels != null && block.functionIndex < functionPanels.Length) ? functionPanels[block.functionIndex] : null;
                        yield return StartCoroutine(ExecuteBlockList(functionLists[block.functionIndex], funcPanel));
                    }
                    break;
                
                case CommandType.If:
                    int ifStart = i + 1;
                    int ifLen = block.innerCommandCount;
                    if (ifStart < blockList.Count) 
                    {
                        ifLen = Mathf.Min(ifLen, blockList.Count - ifStart);
                        if (CheckFrontObstacle(block.conditionObstacle) == block.conditionExpectedState)
                        {
                            List<CommandBlock> subList = blockList.GetRange(ifStart, ifLen);
                            yield return StartCoroutine(ExecuteBlockList(subList, null));
                        }
                    }
                    i += ifLen; // 부모 루프 건너뜀
                    break;

                case CommandType.While:
                    int wStart = i + 1;
                    int wLen = block.innerCommandCount;
                    if (wStart < blockList.Count)
                    {
                        wLen = Mathf.Min(wLen, blockList.Count - wStart);
                        int safeBreak = 0;
                        List<CommandBlock> subList = blockList.GetRange(wStart, wLen);
                        
                        while (CheckFrontObstacle(block.conditionObstacle) == block.conditionExpectedState)
                        {
                            yield return StartCoroutine(ExecuteBlockList(subList, null));
                            safeBreak++;
                            if (safeBreak > 100) { Debug.LogWarning("무한루프 방지"); break; }
                            if (!isExecuting) yield break;
                        }
                    }
                    i += wLen; // 부모 루프 건너뜀
                    break;
            }

            if (!CheckGameState()) yield break;
            if (panel != null) HighlightIcon(panel, i, false);
        }
    }

    private bool CheckFrontObstacle(ObstacleType obsType)
    {
        if (obsType == ObstacleType.None) return true;

        // 낭떠러지(Cliff) 검사: 앞 칸 바닥에 길이 없으면 낭떠러지로 판단
        if (obsType == ObstacleType.Cliff)
        {
            Vector3 nextPos = transform.position + transform.forward * moveStep;
            bool hasGround = Physics.Raycast(nextPos, Vector3.down, groundCheckDistance, roadLayer);
            return !hasGround; // 바닥이 없으면(false) 낭떠러지가 맞음(true)
        }

        // 일반 장애물 검사: 정면 레이캐스트
        RaycastHit hit;
        bool hasObstacle = Physics.Raycast(transform.position, transform.forward, out hit, moveStep);
        
        // 기존의 obstacleTag("Obstacle")를 그대로 유지하면서 이름으로 종류를 판별합니다.
        if (hasObstacle && hit.collider.CompareTag(obstacleTag))
        {
            string objName = hit.collider.gameObject.name.ToLower(); // 소문자로 변환하여 검사
            
            if (obsType == ObstacleType.Tree && objName.Contains("tree")) return true;
            if (obsType == ObstacleType.Box && objName.Contains("box")) return true;
            if (obsType == ObstacleType.Rock && objName.Contains("rock")) return true;
        }
        return false;
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
        if (AIManager.Instance != null)
        {
            AIManager.Instance.ReportOutOfBounds();
        }
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
            if (commandSequencePanel != null) commandSequencePanel.SetActive(false);
            if (limitText != null) limitText.gameObject.SetActive(false);
            if (limitBox != null) limitBox.SetActive(false);
            if (functionPanels != null)
            {
                foreach (var p in functionPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }

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
    public void RefreshAllPanels()
    {
        UpdateIcons(commandSequencePanel, mainCommandList);
        if (functionPanels != null)
        {
            for (int i = 0; i < functionPanels.Length; i++)
            {
                if (functionPanels[i] != null && i < functionLists.Count)
                {
                    UpdateIcons(functionPanels[i], functionLists[i]);
                }
            }
        }
        UpdateLimitText();
    }

    private void UpdateIcons(GameObject panel, List<CommandBlock> list)
    {
        if (panel == null || commandSlotPrefab == null) return;

        int panelListIndex = GetListIndexByPanel(panel);
        Transform targetContent = GetPanelContent(panel);
        int cmdIndex = 0;
        for (int i = 0; i < targetContent.childCount; i++)
        {
            Transform child = targetContent.GetChild(i);
            Image img = child.GetComponent<Image>();
            RectTransform rect = child.GetComponent<RectTransform>();

            if (img == null || rect == null) continue;

            if (cmdIndex < list.Count)
            {
                CommandBlock block = list[cmdIndex];
                switch (block.type)
                {
                    case CommandType.Forward: img.sprite = forwardIcon; break;
                    case CommandType.TurnRight: img.sprite = rightIcon; break;
                    case CommandType.TurnLeft: img.sprite = leftIcon; break;
                    case CommandType.CallFunction: 
                        if (functionCallIcons != null && block.functionIndex >= 0 && block.functionIndex < functionCallIcons.Length && functionCallIcons[block.functionIndex] != null)
                        {
                            img.sprite = functionCallIcons[block.functionIndex];
                        }
                        else
                        {
                            img.sprite = loopIcon;
                        }
                        break;
                    case CommandType.If: img.sprite = ifIcon; break;
                    case CommandType.While: img.sprite = whileIcon; break;
                }
                rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                
                BindSlotEvents(child, list, cmdIndex, panel);
                UpdateSlotVisuals(child, img, list, cmdIndex, panel);

                cmdIndex++;
            }
            else
            {
                // 빈 슬롯 처리: 메인 패널은 기본 emptySlotSprite 사용, F1~F3은 배열 사용
                if (panelListIndex == -1) // 메인 패널
                {
                    img.sprite = emptySlotSprite;
                }
                else // F1, F2, F3 패널
                {
                    if (emptySlotSprites != null && panelListIndex >= 0 && panelListIndex < emptySlotSprites.Length && emptySlotSprites[panelListIndex] != null)
                    {
                        img.sprite = emptySlotSprites[panelListIndex];
                    }
                    else
                    {
                        img.sprite = emptySlotSprite; // 할당 안 되어있으면 기본값 폴백
                    }
                }
                
                rect.sizeDelta = new Vector2(defaultSlotWidth, rect.sizeDelta.y);
                img.color = Color.white;
                ClearSlotEvents(child, panel);
            }
        }
    }

    private void BindSlotEvents(Transform child, List<CommandBlock> list, int index, GameObject panel)
    {
        MouseButtonHandler handler = child.GetComponent<MouseButtonHandler>();
        if (handler == null) handler = child.gameObject.AddComponent<MouseButtonHandler>();

        handler.onLeftClick.RemoveAllListeners();
        handler.onRightClick.RemoveAllListeners();

        int capturedIndex = index;
        handler.onLeftClick.AddListener(() => OnSlotClicked(list, capturedIndex, panel));
        // handler.onRightClick.AddListener(() => OnSlotRightClicked(list, capturedIndex, panel)); // 우클릭 삭제 비활성화

        SlotDragHandler dragHandler = child.GetComponent<SlotDragHandler>();
        if (dragHandler == null) dragHandler = child.gameObject.AddComponent<SlotDragHandler>();
        dragHandler.playerController = this;
        dragHandler.panel = panel;
        dragHandler.commandList = list;
        dragHandler.slotIndex = capturedIndex;

        Transform scopeBtnTr = child.Find("Scope_correction");
        if (scopeBtnTr != null)
        {
            Button scopeBtn = scopeBtnTr.GetComponent<Button>();
            if (scopeBtn != null)
            {
                scopeBtn.onClick.RemoveAllListeners();
                scopeBtn.onClick.AddListener(() => StartScopeEdit(list, capturedIndex, panel));
            }
            
            // Scope_correction 버튼은 If/While 블록일 때만 보여야 합니다.
            CommandBlock block = list[index];
            scopeBtn.gameObject.SetActive(block.type == CommandType.If || block.type == CommandType.While);
        }

        Transform obsBtnTr = child.Find("ConditionObstacle_Btn");
        if (obsBtnTr != null)
        {
            Button obsBtn = obsBtnTr.GetComponent<Button>();
            if (obsBtn != null)
            {
                obsBtn.onClick.RemoveAllListeners();
                obsBtn.onClick.AddListener(() => CycleConditionObstacle(list, capturedIndex, panel));
            }
            CommandBlock block = list[index];
            obsBtnTr.gameObject.SetActive(block.type == CommandType.If || block.type == CommandType.While);
        }

        Transform stateBtnTr = child.Find("ConditionState_Btn");
        if (stateBtnTr != null)
        {
            Button stateBtn = stateBtnTr.GetComponent<Button>();
            if (stateBtn != null)
            {
                stateBtn.onClick.RemoveAllListeners();
                stateBtn.onClick.AddListener(() => ToggleConditionState(list, capturedIndex, panel));
            }
            CommandBlock block = list[index];
            stateBtnTr.gameObject.SetActive(block.type == CommandType.If || block.type == CommandType.While);
        }
    }

    private void ClearSlotEvents(Transform child, GameObject panel)
    {
        MouseButtonHandler handler = child.GetComponent<MouseButtonHandler>();
        if (handler == null) handler = child.gameObject.AddComponent<MouseButtonHandler>();

        if (handler != null)
        {
            handler.onLeftClick.RemoveAllListeners();
            handler.onRightClick.RemoveAllListeners();
            
            handler.onLeftClick.AddListener(() => {
                if (!isExecuting && !isEditingScope)
                {
                    SetActivePanel(GetListIndexByPanel(panel));
                }
            });
        }

        SlotDragHandler dragHandler = child.GetComponent<SlotDragHandler>();
        if (dragHandler != null) Destroy(dragHandler);

        Transform scopeBtnTr = child.Find("Scope_correction");
        if (scopeBtnTr != null) scopeBtnTr.gameObject.SetActive(false);

        Transform obsBtnTr = child.Find("ConditionObstacle_Btn");
        if (obsBtnTr != null) obsBtnTr.gameObject.SetActive(false);

        Transform stateBtnTr = child.Find("ConditionState_Btn");
        if (stateBtnTr != null) stateBtnTr.gameObject.SetActive(false);
    }

    private void CycleConditionObstacle(List<CommandBlock> list, int index, GameObject panel)
    {
        if (isExecuting) return;
        CommandBlock block = list[index];
        
        if (block.conditionObstacle == ObstacleType.None) block.conditionObstacle = ObstacleType.Tree;
        else if (block.conditionObstacle == ObstacleType.Tree) block.conditionObstacle = ObstacleType.Box;
        else if (block.conditionObstacle == ObstacleType.Box) block.conditionObstacle = ObstacleType.Rock;
        else if (block.conditionObstacle == ObstacleType.Rock) block.conditionObstacle = ObstacleType.Cliff;
        else if (block.conditionObstacle == ObstacleType.Cliff) block.conditionObstacle = ObstacleType.Tree;

        RefreshAllPanels();
    }

    private void ToggleConditionState(List<CommandBlock> list, int index, GameObject panel)
    {
        if (isExecuting) return;
        CommandBlock block = list[index];
        block.conditionExpectedState = !block.conditionExpectedState;
        
        RefreshAllPanels();
    }

    private void StartScopeEdit(List<CommandBlock> list, int index, GameObject panel)
    {
        if (isExecuting) return;
        
        // 클릭 모드 진입
        isEditingScope = true;
        editingScopeStartIndex = index;
        editingScopeList = list;
        editingScopePanel = panel;
        
        Debug.Log($"[Scope Edit] {index}번 블록의 범위 설정을 시작합니다. 닫을 마지막 블록을 클릭하세요.");
        RefreshAllPanels();
    }

    private void OnSlotRightClicked(List<CommandBlock> list, int index, GameObject panel)
    {
        if (isExecuting || isEditingScope) return;
        
        list.RemoveAt(index);
        insertIndex = -1; // 삭제 시 삽입점 초기화
        SoundManager.Instance?.PlayCommandClick();
        RefreshAllPanels();
    }

    private void OnSlotClicked(List<CommandBlock> list, int index, GameObject panel)
    {
        if (isExecuting) return;

        if (isEditingScope && editingScopeList == list && panel == editingScopePanel)
        {
            if (index >= editingScopeStartIndex)
            {
                int scopeSize = index - editingScopeStartIndex;
                editingScopeList[editingScopeStartIndex].innerCommandCount = scopeSize;
                isEditingScope = false;
                insertIndex = index + 1; // 범위 설정 후 그 다음 위치에 커맨드가 들어가도록 자동 설정
                RefreshAllPanels();
            }
            else
            {
                Debug.LogWarning("[Scope Edit] 마지막 블록은 시작 블록(If/While)보다 뒤에 있어야 합니다.");
            }
        }
        else if (!isEditingScope)
        {
            activeListIndex = GetListIndexByPanel(panel);
            // insertIndex = index; // <--- 중간 삽입 위치 지정(클릭) 기능 삭제! 이제 무조건 맨 끝에 추가됩니다.
            RefreshAllPanels();
        }
    }

    private int GetListIndexByPanel(GameObject panel)
    {
        if (panel == commandSequencePanel) return -1;
        if (functionPanels != null)
        {
            for (int i = 0; i < functionPanels.Length; i++)
            {
                if (panel == functionPanels[i]) return i;
            }
        }
        return -1;
    }

    private void UpdateSlotVisuals(Transform child, Image img, List<CommandBlock> list, int index, GameObject panel)
    {
        CommandBlock block = list[index];
        img.color = Color.white; 

        // 범위 수정 모드 하이라이트
        if (isEditingScope && list == editingScopeList && index == editingScopeStartIndex)
        {
            img.color = Color.yellow; 
            return;
        }

        // 삽입 지점(Insert Target) 하이라이트 (빨간색)
        int listIndex = GetListIndexByPanel(panel);
        if (!isEditingScope && listIndex == activeListIndex && index == insertIndex)
        {
            img.color = new Color(1f, 0.7f, 0.7f); // 연한 빨간색
        }

        // 다른 블록의 Scope(테두리) 안에 속해있는지 확인
        bool isInsideScope = false;
        for (int i = 0; i < index; i++)
        {
            CommandBlock prev = list[i];
            if (prev.type == CommandType.If || prev.type == CommandType.While)
            {
                if (i + prev.innerCommandCount >= index)
                {
                    isInsideScope = true;
                    break;
                }
            }
        }

        if (isInsideScope)
        {
            img.color = new Color(0.8f, 0.9f, 1f); // 약간 파란색 틴트 (If/While 범위 내부에 있음)
        }

        // -------------------------
        // 조건 토글 아이콘 시각화 업데이트
        // -------------------------
        if (block.type == CommandType.If || block.type == CommandType.While)
        {
            Transform obsBtnTr = child.Find("ConditionObstacle_Btn");
            if (obsBtnTr != null)
            {
                Image obsImg = obsBtnTr.GetComponent<Image>();
                if (obsImg != null)
                {
                    switch (block.conditionObstacle)
                    {
                        case ObstacleType.Tree: obsImg.sprite = obsTreeIcon; break;
                        case ObstacleType.Box: obsImg.sprite = obsBoxIcon; break;
                        case ObstacleType.Rock: obsImg.sprite = obsRockIcon; break;
                        case ObstacleType.Cliff: obsImg.sprite = obsCliffIcon; break;
                        default: obsImg.sprite = obsTreeIcon; break;
                    }
                }
            }

            Transform stateBtnTr = child.Find("ConditionState_Btn");
            if (stateBtnTr != null)
            {
                Image stateImg = stateBtnTr.GetComponent<Image>();
                if (stateImg != null)
                {
                    stateImg.sprite = block.conditionExpectedState ? stateTrueIcon : stateFalseIcon;
                }
            }
        }
    }

    private void HighlightIcon(GameObject panel, int index, bool highlight)
    {
        if (panel == null) return;
        Transform targetContent = GetPanelContent(panel);
        if (index < 0 || index >= targetContent.childCount) return;
        Transform tr = targetContent.GetChild(index);
        tr.localScale = highlight ? Vector3.one * highlightScale : Vector3.one;
    }

    private int GetCurrentCost()
    {
        int total = mainCommandList.Count;
        foreach (var funcList in functionLists)
        {
            if (funcList.Count > 0) total += 1 + funcList.Count;
        }
        return total;
    }

    private void UpdateLimitText()
    {
        if (limitText == null) return;
        int current = GetCurrentCost();
        limitText.text = $"{current} / {maxCommandCost}";
        limitText.color = (current >= maxCommandCost) ? Color.red : Color.black;
    }



    // ---------------------------------------------------------
    // 드래그 앤 드롭 지원 (Drag & Drop)
    // ---------------------------------------------------------
    public void HandleDropInsert(UnityEngine.EventSystems.PointerEventData eventData, CommandBlock block)
    {
        GameObject hitPanel = null;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (commandSequencePanel != null && result.gameObject.transform.IsChildOf(commandSequencePanel.transform))
            {
                hitPanel = commandSequencePanel;
                break;
            }
            if (functionPanels != null)
            {
                foreach (var fPanel in functionPanels)
                {
                    if (fPanel != null && result.gameObject.transform.IsChildOf(fPanel.transform))
                    {
                        hitPanel = fPanel;
                        break;
                    }
                }
            }
            if (hitPanel != null) break;
        }

        if (hitPanel != null)
        {
            isPanelsUp = false;
            GameObject targetPanel = activeListIndex == -1 ? commandSequencePanel : functionPanels[activeListIndex];
            int listIndex = GetListIndexByPanel(targetPanel);
            Debug.Log($"[DragDrop] listIndex: {listIndex}");
            if (listIndex >= -1)
            {
                List<CommandBlock> targetList = listIndex == -1 ? mainCommandList : functionLists[listIndex];
                
                int currentCost = GetCurrentCost();
                if (currentCost >= maxCommandCost)
                {
                    SoundManager.Instance?.PlayBump();
                    return;
                }

                Transform contentTr = GetPanelContent(targetPanel);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)contentTr, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                
                float panelLocalX = -289.2f;
                float intervalX = 64.5f;
                
                Transform referencePanel = contentTr;
                if (contentTr.parent != null && contentTr.parent.name == "Viewport") referencePanel = contentTr.parent.parent;
                
                Vector3 worldPos = referencePanel.TransformPoint(new Vector3(panelLocalX, 0, 0));
                Vector3 startLocalPos = contentTr.InverseTransformPoint(worldPos);
                
                int dropIndex = Mathf.RoundToInt((localPoint.x - startLocalPos.x) / intervalX);
                dropIndex = Mathf.Clamp(dropIndex, 0, targetList.Count);

                targetList.Insert(dropIndex, block);
                
                if (listIndex == activeListIndex && insertIndex >= dropIndex)
                {
                    insertIndex++;
                }
                
                RefreshAllPanels();
                UpdateLimitText();
                SoundManager.Instance?.PlayCommandClick();
            }
        }
    }

    public void HandleDropSlot(UnityEngine.EventSystems.PointerEventData eventData, GameObject originPanel, List<CommandBlock> originList, int originIndex)
    {
        GameObject hitPanel = null;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (commandSequencePanel != null && result.gameObject.transform.IsChildOf(commandSequencePanel.transform))
            {
                hitPanel = commandSequencePanel;
                break;
            }
            if (functionPanels != null)
            {
                foreach (var fPanel in functionPanels)
                {
                    if (fPanel != null && result.gameObject.transform.IsChildOf(fPanel.transform))
                    {
                        hitPanel = fPanel;
                        break;
                    }
                }
            }
            if (hitPanel != null) break;
        }

        // 만약 패널 바깥으로 드래그 앤 드롭했다면 삭제
        if (hitPanel == null)
        {
            if (originIndex >= 0 && originIndex < originList.Count)
            {
                originList.RemoveAt(originIndex);
                
                int panelIdx = GetListIndexByPanel(originPanel);
                if (panelIdx == activeListIndex)
                {
                    insertIndex = -1; // 삭제 시 삽입점 초기화
                }
                
                if (isEditingScope && editingScopeList == originList)
                {
                    isEditingScope = false;
                    editingScopeList = null;
                }

                RefreshAllPanels();
                UpdateLimitText();
                SoundManager.Instance?.PlayCommandClick();
            }
        }
        else
        {
            // 어떠한 패널이든 위에서 드롭했다면 (빈틈으로 인해 뒤쪽 패널이 맞았더라도)
            // 무조건 출발했던 originPanel 안에서 순서가 변경된 것으로 간주
            GameObject targetPanel = originPanel;

            if (originIndex >= 0 && originIndex < originList.Count)
            {
                CommandBlock blockToMove = originList[originIndex];
                int targetListIndex = GetListIndexByPanel(targetPanel);
                
                if (targetListIndex >= -1)
                {
                    List<CommandBlock> targetList = targetListIndex == -1 ? mainCommandList : functionLists[targetListIndex];
                    
                    Transform contentTr = GetPanelContent(targetPanel);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)contentTr, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                    
                    float panelLocalX = -289.2f;
                    float intervalX = 64.5f;
                    
                    Transform referencePanel = contentTr;
                    if (contentTr.parent != null && contentTr.parent.name == "Viewport") referencePanel = contentTr.parent.parent;
                    
                    Vector3 worldPos = referencePanel.TransformPoint(new Vector3(panelLocalX, 0, 0));
                    Vector3 startLocalPos = contentTr.InverseTransformPoint(worldPos);
                    
                    int dropIndex = Mathf.RoundToInt((localPoint.x - startLocalPos.x) / intervalX);
                    
                    originList.RemoveAt(originIndex);
                    
                    dropIndex = Mathf.Clamp(dropIndex, 0, targetList.Count);
                    targetList.Insert(dropIndex, blockToMove);

                    RefreshAllPanels();
                    UpdateLimitText();
                    SoundManager.Instance?.PlayCommandClick();
                }
            }
        }
    }
    
    public void ToggleUIVisibility(bool isVisible)
    {
        if (inGameUIGroup != null) inGameUIGroup.SetActive(isVisible);
        if (commandSequencePanel != null) commandSequencePanel.SetActive(isVisible);
        if (limitText != null) limitText.gameObject.SetActive(isVisible);
        if (limitBox != null) limitBox.SetActive(isVisible);
        if (functionPanels != null)
        {
            foreach (var p in functionPanels)
            {
                if (p != null) p.SetActive(isVisible);
            }
        }
    }
}
