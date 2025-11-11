using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 레거시 UI(Text)를 사용하기 위해 필요합니다.

public class PlayerController : MonoBehaviour
{
    // --- 명령 관련 변수 ---
    public enum CommandType { Forward, TurnRight, TurnLeft }
    private List<CommandType> commandList = new List<CommandType>();

    // --- UI 참조 변수 ---
    [Header("UI 연결 (자동 연결됨)")]
    // (UI_Auto_Connector가 자동으로 연결해 줌)
    private Text commandSequenceText;
    private GameObject successPanel;

    // --- 오브젝트 참조 변수 ---
    [Header("오브젝트 연결 (씬마다 수동 연결)")]
    public GameObject startBox; // 씬마다 연결 필요
    public GameObject endPoint; // 씬마다 연결 필요

    // --- 플레이어 상태 변수 ---
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isExecuting = false;

    // --- 이동 설정값 ---
    [Header("이동 설정")]
    public float moveStep = 1.0f;
    public float turnStep = 90.0f;
    public float moveDuration = 0.5f;
    public float turnDuration = 0.3f;
    public float bumpForce = 0.2f;
    public float bumpDuration = 0.15f;

    // --- 땅 감지(Raycast) 설정 ---
    [Header("땅 감지 설정")]
    public LayerMask roadLayer; // 인스펙터에서 "Road" 레이어 선택
    public float groundCheckDistance = 2.0f;

    // --- 태그 설정 ---
    [Header("태그 이름 (문자열)")]
    public string obstacleTag = "Obstacle";


    void Start()
    {
        // 1. 시작 위치 계산 및 저장
        // (startBox가 public이므로 연결되어 있어야 함)
        startPosition = new Vector3(
            startBox.transform.position.x,
            startBox.transform.position.y + 1.7f,
            startBox.transform.position.z
        );

        // 2. 시작 회전값 저장
        startRotation = transform.rotation;

        // 3. 위치만 초기화 (UI 관련 초기화는 InitializeUI에서 수행)
        transform.position = startPosition;
        transform.rotation = startRotation;

        // (중요) ResetPlayer()는 UI가 연결된 후 InitializeUI()에서 호출됩니다.
    }

    /// <summary>
    /// (새로 추가된 함수)
    /// UI_Auto_Connector가 이 함수를 호출하여 UI 요소들을 주입합니다.
    /// </summary>
    public void InitializeUI(Text cmdText, GameObject successPnl)
    {
        // 1. UI 참조를 전달받습니다.
        this.commandSequenceText = cmdText;
        this.successPanel = successPnl;

        // 2. UI 참조가 확정된 이 시점에, UI를 사용하는 초기화 함수를 호출합니다.
        ResetPlayer();
    }


    #region 1. 버튼 연결 함수 (UI에서 호출)

    public void AddCommand_Forward()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.Forward);
        UpdateCommandText();
    }

    public void AddCommand_TurnRight()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.TurnRight);
        UpdateCommandText();
    }

    public void AddCommand_TurnLeft()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.TurnLeft);
        UpdateCommandText();
    }

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

    #endregion

    #region 2. 명령 실행 로직 (코루틴)

    // 땅(Road 레이어) 위에 있는지 확인하는 함수
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, roadLayer);
    }

    IEnumerator ExecuteSequence()
    {
        isExecuting = true;

        if (!IsGrounded())
        {
            Debug.Log("시작 지점이 'Road' 레이어 위에 있지 않습니다!");
            ResetPlayer();
            yield break;
        }

        foreach (CommandType cmd in commandList)
        {
            switch (cmd)
            {
                case CommandType.Forward:
                    yield return StartCoroutine(MoveForward());
                    break;
                case CommandType.TurnRight:
                    yield return StartCoroutine(Turn(turnStep));
                    break;
                case CommandType.TurnLeft:
                    yield return StartCoroutine(Turn(-turnStep));
                    break;
            }

            yield return new WaitForSeconds(0.1f);

            if (!IsGrounded())
            {
                Debug.Log("길을 벗어났습니다! (Raycast 실패)");
                ResetPlayer();
                yield break;
            }
        }

        isExecuting = false;
        CheckForWin();
    }

    IEnumerator MoveForward()
    {
        RaycastHit hit;
        bool hasObstacle = Physics.Raycast(
            transform.position,
            transform.forward,
            out hit,
            moveStep
        );

        if (hasObstacle && hit.collider.CompareTag(obstacleTag))
        {
            Debug.Log("나무에 부딪혔습니다!");
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

    #endregion

    #region 3. 상태 관리 및 UI/충돌 처리

    private void UpdateCommandText()
    {
        // UI가 아직 연결되기 전에 호출될 수 있으므로 확인
        if (commandSequenceText == null)
        {
            // Debug.LogWarning("Command Sequence Text가 아직 연결되지 않았습니다.");
            return;
        }

        string text = "명령 순서: ";
        if (commandList.Count == 0)
        {
            text += "(명령을 추가하세요)";
        }
        else
        {
            foreach (CommandType cmd in commandList)
            {
                switch (cmd)
                {
                    case CommandType.Forward:
                        text += "전진 → ";
                        break;
                    case CommandType.TurnRight:
                        text += "우회전 ↻ ";
                        break;
                    case CommandType.TurnLeft:
                        text += "좌회전 ↺ ";
                        break;
                }
            }
        }
        commandSequenceText.text = text;
    }

    private void CheckForWin()
    {
        if (endPoint == null || successPanel == null) return;
        Vector3 playerPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 endPointPosXZ = new Vector3(endPoint.transform.position.x, 0, endPoint.transform.position.z);

        if (Vector3.Distance(playerPosXZ, endPointPosXZ) < 0.1f)
        {
            Debug.Log("성공!");
            successPanel.SetActive(true);
        }
    }

    private void ResetPlayerPosition()
    {
        StopAllCoroutines();
        isExecuting = false;
        transform.position = startPosition;
        transform.rotation = startRotation;

        // (수정) null 조건부 연산자 '?' 추가
        // UI가 연결되기 전(Start)이나 연결 실패 시에도 오류가 나지 않도록 함
        successPanel?.SetActive(false);
    }

    private void ResetPlayer()
    {
        ResetPlayerPosition(); // 위치, 회전 리셋
        commandList.Clear();
        UpdateCommandText(); // UI 텍스트 초기화
    }

    #endregion
}