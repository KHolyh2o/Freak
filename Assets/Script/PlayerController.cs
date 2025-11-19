using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    // --- 아이콘 스프라이트 ---
    [Header("아이콘 스프라이트")]
    public Sprite forwardIcon;
    public Sprite rightIcon;
    public Sprite leftIcon;
    public Sprite loopIcon;

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

    // --- 감지 설정 ---
    [Header("감지 설정")]
    public LayerMask roadLayer;
    public float groundCheckDistance = 2.0f;
    public string obstacleTag = "Obstacle";
    public int loopRepeatCount = 2;

    private SoundManager soundManager;

    void Start()
    {
        if (startBox == null) return;

        startPosition = new Vector3(
            startBox.transform.position.x,
            startBox.transform.position.y + 1.33f,
            startBox.transform.position.z
        );
        startRotation = transform.rotation;

        transform.position = startPosition;
        transform.rotation = startRotation;

        soundManager = FindObjectOfType<SoundManager>();
    }

    // UI 초기화
    public void InitializeUI(GameObject mainPanel, GameObject loopPanel, GameObject slotPrefab, GameObject successPnl, GameObject loopPopup)
    {
        this.commandSequencePanel = mainPanel;
        this.loopSequencePanel = loopPanel;
        this.commandSlotPrefab = slotPrefab;
        this.successPanel = successPnl;
        this.loopConfigPopup = loopPopup;

        ResetPlayer(); // 여기서 호출됨
    }

    // ---------------------------------------------------------
    // 1. 스마트 버튼 기능
    // ---------------------------------------------------------

    public void AddCommand_Forward()
    {
        if (isExecuting) return;
        if (IsLoopPopupActive()) AddToConfig(CommandType.Forward);
        else AddToMain(CommandType.Forward);
    }

    public void AddCommand_TurnRight()
    {
        if (isExecuting) return;
        if (IsLoopPopupActive()) AddToConfig(CommandType.TurnRight);
        else AddToMain(CommandType.TurnRight);
    }

    public void AddCommand_TurnLeft()
    {
        if (isExecuting) return;
        if (IsLoopPopupActive()) AddToConfig(CommandType.TurnLeft);
        else AddToMain(CommandType.TurnLeft);
    }

    public void AddCommand_Loop_ToMain()
    {
        if (isExecuting) return;
        AddToMain(CommandType.Loop);
        soundManager?.PlayCommandClick();
    }

    private bool IsLoopPopupActive()
    {
        return loopConfigPopup != null && loopConfigPopup.activeSelf;
    }

    private void AddToMain(CommandType type)
    {
        mainCommandList.Add(type);
        UpdateIcons(commandSequencePanel, mainCommandList);
        soundManager?.PlayCommandClick();
    }

    private void AddToConfig(CommandType type)
    {
        loopCommandConfig.Add(type);
        UpdateIcons(loopSequencePanel, loopCommandConfig);
        soundManager?.PlayCommandClick();
    }

    // ---------------------------------------------------------
    // 2. 팝업창 관리 기능
    // ---------------------------------------------------------

    public void OpenLoopConfigPopup()
    {
        if (isExecuting) return;
        if (loopConfigPopup != null)
        {
            loopConfigPopup.SetActive(true);
            UpdateIcons(loopSequencePanel, loopCommandConfig);
        }
    }

    public void CloseLoopConfigPopup()
    {
        if (loopConfigPopup != null) loopConfigPopup.SetActive(false);
    }

    public void ClearLoopConfig()
    {
        if (isExecuting) return;
        loopCommandConfig.Clear();
        UpdateIcons(loopSequencePanel, loopCommandConfig);
        soundManager?.PlayResetClick();
    }

    // ---------------------------------------------------------
    // 3. 실행 및 리셋 (여기에 누락되었던 ResetPlayer 추가함)
    // ---------------------------------------------------------

    public void ExecuteCommands()
    {
        if (isExecuting) return;
        ResetPlayerPosition();
        StartCoroutine(ExecuteSequence());
    }

    public void ResetGame()
    {
        ResetPlayer();
    }

    // ★ 누락되었던 함수 복구 완료 ★
    private void ResetPlayer()
    {
        ResetPlayerPosition();
        mainCommandList.Clear();
        // loopCommandConfig는 유지함
        UpdateIcons(commandSequencePanel, mainCommandList);
    }

    private void ResetPlayerPosition()
    {
        StopAllCoroutines();
        isExecuting = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        if (successPanel != null) successPanel.SetActive(false);
    }


    // ---------------------------------------------------------
    // 4. 실행 로직
    // ---------------------------------------------------------

    IEnumerator ExecuteSequence()
    {
        isExecuting = true;

        if (!IsGrounded())
        {
            Debug.Log("시작 지점 오류");
            soundManager?.PlayFall();
            ResetPlayer();
            yield break;
        }

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
                        yield return StartCoroutine(ProcessSingleCommand(subCmd));
                        yield return new WaitForSeconds(0.1f);

                        if (!isExecuting) yield break;
                        if (!IsGrounded()) { FailSequence(); yield break; }
                    }
                }
            }
            else
            {
                yield return StartCoroutine(ProcessSingleCommand(cmd));
                yield return new WaitForSeconds(0.1f);

                if (!isExecuting) yield break;
                if (!IsGrounded()) { FailSequence(); yield break; }
            }

            HighlightIcon(commandSequencePanel, index, false);
            index++;
        }

        isExecuting = false;
    }

    IEnumerator ProcessSingleCommand(CommandType cmd)
    {
        soundManager?.PlayStep();
        switch (cmd)
        {
            case CommandType.Forward: yield return StartCoroutine(MoveForward()); break;
            case CommandType.TurnRight: yield return StartCoroutine(Turn(turnStep)); break;
            case CommandType.TurnLeft: yield return StartCoroutine(Turn(-turnStep)); break;
        }
    }

    void FailSequence()
    {
        Debug.Log("실패!");
        soundManager?.PlayFall();
        ResetPlayer();
    }

    // ---------------------------------------------------------
    // 5. 물리 이동 로직
    // ---------------------------------------------------------
    IEnumerator MoveForward()
    {
        RaycastHit hit;
        bool hasObstacle = Physics.Raycast(transform.position, transform.forward, out hit, moveStep);

        if (hasObstacle && hit.collider.CompareTag(obstacleTag))
        {
            Debug.Log("장애물 충돌!");
            soundManager?.PlayBump();

            Vector3 originalPos = transform.position;
            Vector3 bumpTargetPos = originalPos - transform.forward * bumpForce;
            float elapsedTime = 0;
            while (elapsedTime < bumpDuration)
            {
                transform.position = Vector3.Lerp(bumpTargetPos, originalPos, elapsedTime / bumpDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = originalPos;
            yield break;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * moveStep;
        float elapsedTimeMove = 0;

        while (elapsedTimeMove < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTimeMove / moveDuration);
            elapsedTimeMove += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }

    IEnumerator Turn(float angle)
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = transform.rotation * Quaternion.Euler(0, angle, 0);
        float elapsedTime = 0;

        while (elapsedTime < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsedTime / turnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRot;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, roadLayer);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EndPoint"))
        {
            if (isExecuting)
            {
                Debug.Log("성공!");
                soundManager?.PlaySuccess();
                if (successPanel != null) successPanel.SetActive(true);
                StopAllCoroutines();
                isExecuting = false;
            }
        }
    }

    // ---------------------------------------------------------
    // 6. UI 업데이트
    // ---------------------------------------------------------
    private void UpdateIcons(GameObject panel, List<CommandType> list)
    {
        if (panel == null || commandSlotPrefab == null) return;

        foreach (Transform child in panel.transform) Destroy(child.gameObject);

        foreach (CommandType cmd in list)
        {
            GameObject slot = Instantiate(commandSlotPrefab, panel.transform);
            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                switch (cmd)
                {
                    case CommandType.Forward: img.sprite = forwardIcon; break;
                    case CommandType.TurnRight: img.sprite = rightIcon; break;
                    case CommandType.TurnLeft: img.sprite = leftIcon; break;
                    case CommandType.Loop: img.sprite = loopIcon; break;
                }
                img.color = (img.sprite != null) ? Color.white : Color.gray;
            }
        }
    }

    private void HighlightIcon(GameObject panel, int index, bool highlight)
    {
        if (panel == null || index < 0 || index >= panel.transform.childCount) return;
        Transform tr = panel.transform.GetChild(index);
        tr.localScale = highlight ? Vector3.one * highlightScale : Vector3.one;
    }
}