using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // --- 명령 관련 변수 ---
    public enum CommandType { Forward, TurnRight, TurnLeft }
    private List<CommandType> commandList = new List<CommandType>();

    // --- UI 참조 변수 ---
    [Header("UI 연결")]
    public Text commandSequenceText;
    public GameObject successPanel;

    // --- 오브젝트 참조 변수 ---
    [Header("오브젝트 연결")]
    public GameObject startBox;
    public GameObject endPoint;

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
    public LayerMask roadLayer; // "Road" 레이어를 감지하기 위한 설정
    public float groundCheckDistance = 2.0f; // 플레이어 발밑 2.0f 거리까지 "Road"를 찾음

    // --- 태그 설정 ---
    [Header("태그 이름 (문자열)")]
    public string obstacleTag = "Obstacle";


    void Start()
    {
        startPosition = new Vector3(
            startBox.transform.position.x,
            startBox.transform.position.y + 1.7f,
            startBox.transform.position.z
        );
        startRotation = transform.rotation;

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
        // 플레이어 위치(transform.position)에서 아래(Vector3.down)로
        // groundCheckDistance 만큼 레이저를 쏴서
        // roadLayer와 부딪히는지 확인
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, roadLayer);
    }

    IEnumerator ExecuteSequence()
    {
        isExecuting = true;

        // (수정) 실행 시작 시점에 땅(Road) 위에 있는지 먼저 확인
        if (!IsGrounded())
        {
            Debug.Log("시작 지점이 'Road' 레이어 위에 있지 않습니다!");
            ResetPlayer();
            yield break; // 실행 중지
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

            yield return new WaitForSeconds(0.1f); // 동작이 끝날 시간을 줌

            // --- (중요) 수정된 길 벗어남 감지 ---
            // 매 동작이 끝난 후, 발밑에 "Road" 레이어가 있는지 확인
            if (!IsGrounded())
            {
                Debug.Log("길을 벗어났습니다! (Raycast 실패)");

                // (선택 사항) 떨어지는 연출을 위해 잠시 대기
                // yield return new WaitForSeconds(0.5f); 

                ResetPlayer(); // 플레이어 리셋
                yield break;  // 코루틴(명령 실행) 즉시 중단
            }
        }

        isExecuting = false;

        // 모든 명령이 끝나고도 길 위에 있다면 성공 체크
        CheckForWin();
    }

    // ... (MoveForward, Turn 함수는 이전과 동일) ...
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

    // (수정) 텍스트가 안 나올 경우를 대비해 switch-case 문 포함
    private void UpdateCommandText()
    {
        if (commandSequenceText == null)
        {
            Debug.LogWarning("Command Sequence Text가 연결되지 않았습니다!");
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
        if (successPanel != null) successPanel.SetActive(false);
    }

    private void ResetPlayer()
    {
        ResetPlayerPosition();
        commandList.Clear();
        UpdateCommandText();
    }

    // --- (삭제) ---
    // OnTriggerEnter, OnTriggerExit, isOnRoad 변수 등은
    // 모두 삭제되었습니다. (더 이상 필요 없음)

    #endregion
}