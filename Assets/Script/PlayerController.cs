using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 레거시 UI(Text)를 사용하기 위해 필요합니다.

public class PlayerController : MonoBehaviour
{
    [Header("UI 하이라이트 설정")]
    public float highlightScale = 1.2f; // 1.2배 커짐 (기본값)

    // --- 명령 관련 변수 ---
    public enum CommandType { Forward, TurnRight, TurnLeft }
    private List<CommandType> commandList = new List<CommandType>();

    // --- UI 참조 변수 ---
    [Header("UI 연결 (자동 연결됨)")]
    private GameObject commandSequencePanel; // (UI_Auto_Connector가 연결)
    private GameObject commandSlotPrefab;    // (UI_Auto_Connector가 연결)
    private GameObject successPanel;         // (UI_Auto_Connector가 연결)

    // --- 명령 아이콘 스프라이트 ---
    [Header("명령 아이콘 스프라이트 (수동 연결)")]
    public Sprite forwardIcon; // (Player_Root의 인스펙터에서 연결 필요)
    public Sprite rightIcon;   // (Player_Root의 인스펙터에서 연결 필요)
    public Sprite leftIcon;    // (Player_Root의 인스펙터에서 연결 필요)

    // --- 오브젝트 참조 변수 ---
    [Header("오브젝트 연결 (씬마다 수동 연결)")]
    public GameObject startBox; // (Player_Root의 인스펙터에서 연결 필요)
    public GameObject endPoint; // (연결은 되어있지만, 이제 충돌 감지용으로 사용됨)

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
    public LayerMask roadLayer; // (Player_Root의 인스펙터에서 "Road" 선택 필요)
    public float groundCheckDistance = 2.0f;

    // --- 태그 설정 ---
    [Header("태그 이름 (문자열)")]
    public string obstacleTag = "Obstacle";

    private SoundManager soundManager;


    void Start()
    {
        // 1. startBox가 연결되어 있는지 확인
        if (startBox == null)
        {
            Debug.LogError("StartBox가 PlayerController에 연결되지 않았습니다!");
            return;
        }

        // 2. 시작 위치 계산 및 저장
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

    /// <summary>
    /// UI_Auto_Connector가 이 함수를 호출하여 UI 요소들을 주입합니다.
    /// </summary>
    public void InitializeUI(GameObject cmdPanel, GameObject cmdSlotPfb, GameObject successPnl)
    {
        this.commandSequencePanel = cmdPanel;
        this.commandSlotPrefab = cmdSlotPfb;
        this.successPanel = successPnl;

        // UI가 모두 준비된 후 플레이어 초기화
        ResetPlayer();
    }


    #region 1. 버튼 연결 함수 (UI에서 호출)

    public void AddCommand_Forward()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.Forward);
        UpdateCommandIcons(); // 이름 변경 (UpdateCommandText -> UpdateCommandIcons)
    }

    public void AddCommand_TurnRight()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.TurnRight);
        UpdateCommandIcons();
    }

    public void AddCommand_TurnLeft()
    {
        if (isExecuting) return;
        commandList.Add(CommandType.TurnLeft);
        UpdateCommandIcons();
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
            soundManager?.PlayFall();
            ResetPlayer();
            yield break;
        }

        int currentCommandIndex = 0;

        foreach (CommandType cmd in commandList)
        {
            // 명령 실행 전 하이라이트
            HighlightCommandIcon(currentCommandIndex, true);

            soundManager?.PlayStep();

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

            // 명령 실행 후 하이라이트 해제
            HighlightCommandIcon(currentCommandIndex, false);
            currentCommandIndex++;

            // (중요) isExecuting가 false가 되었는지 매번 확인 (성공 시 즉시 중단)
            if (!isExecuting)
            {
                yield break; // OnTriggerEnter에서 성공하여 실행이 중지됨
            }

            if (!IsGrounded())
            {
                Debug.Log("길을 벗어났습니다! (Raycast 실패)");
                soundManager?.PlayFall();
                ResetPlayer();
                yield break;
            }
        }

        isExecuting = false;

        // (삭제됨) 좌표 기반의 CheckForWin() 호출 삭제
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

    #endregion

    #region 3. 상태 관리 및 UI/충돌 처리

    /// <summary>
    /// 명령 순서 UI를 아이콘으로 업데이트합니다.
    /// </summary>
    private void UpdateCommandIcons()
    {
        if (commandSequencePanel == null || commandSlotPrefab == null)
        {
            return;
        }

        // 1. 기존 아이콘 모두 삭제
        foreach (Transform child in commandSequencePanel.transform)
        {
            Destroy(child.gameObject);
        }

        // 2. 리스트 기반으로 아이콘 새로 생성
        foreach (CommandType cmd in commandList)
        {
            GameObject commandSlot = Instantiate(commandSlotPrefab, commandSequencePanel.transform);
            Image slotImage = commandSlot.GetComponent<Image>();

            if (slotImage != null)
            {
                switch (cmd)
                {
                    case CommandType.Forward:
                        slotImage.sprite = forwardIcon;
                        break;
                    case CommandType.TurnRight:
                        slotImage.sprite = rightIcon;
                        break;
                    case CommandType.TurnLeft:
                        slotImage.sprite = leftIcon;
                        break;
                }
                slotImage.color = (slotImage.sprite != null) ? Color.white : Color.gray;
            }
        }
    }

    /// <summary>
    /// 실행 중인 명령 아이콘을 하이라이트합니다.
    /// </summary>
    private void HighlightCommandIcon(int index, bool highlight)
    {
        // 1. 패널이나 아이콘이 없으면 리턴
        if (commandSequencePanel == null || index < 0 || index >= commandSequencePanel.transform.childCount)
        {
            return;
        }

        // 2. 해당 순서(index)의 아이콘 오브젝트를 찾음
        Transform commandIconTransform = commandSequencePanel.transform.GetChild(index);

        // --- [색상 변경 코드 삭제됨] ---

        // --- [크기(Scale) 변경] ---
        // highlight가 true면 설정한 비율(highlightScale)만큼 커지고, 아니면 원래 크기(1.0)로 돌아옴
        if (highlight)
        {
            commandIconTransform.localScale = Vector3.one * highlightScale;
        }
        else
        {
            commandIconTransform.localScale = Vector3.one;
        }
    }

    // (★삭제됨★) 
    // private void CheckForWin() { ... } 
    // -> 함수 자체가 필요 없음

    private void ResetPlayerPosition()
    {
        StopAllCoroutines(); // 진행 중인 모든 명령(이동) 중지
        isExecuting = false;
        transform.position = startPosition;
        transform.rotation = startRotation;

        successPanel?.SetActive(false); // successPanel이 null이 아니면 비활성화
    }

    private void ResetPlayer()
    {
        ResetPlayerPosition();
        commandList.Clear();
        UpdateCommandIcons(); // UI 아이콘 초기화
    }

    /// <summary>
    /// (★핵심★)
    /// 플레이어의 콜라이더가 다른 '트리거(Trigger)' 콜라이더에 '진입(Enter)'했을 때 호출됩니다.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // 1. 진입한 트리거의 태그가 "EndPoint"인지 확인합니다.
        // (EndPoint 오브젝트의 Tag를 "EndPoint"로, Collider의 Is Trigger를 true로 설정해야 함)
        if (other.CompareTag("EndPoint"))
        {
            // 2. 명령이 실행 중일 때만 성공으로 처리 (중복 방지)
            if (isExecuting)
            {
                Debug.Log("성공! (OnTriggerEnter 감지)");

                soundManager?.PlaySuccess();

                // 3. 성공 패널 활성화
                if (successPanel != null)
                {
                    successPanel.SetActive(true);
                }

                // 4. (중요) 성공했으므로, 남아있는 명령(코루틴)을 모두 중지합니다.
                StopAllCoroutines();
                isExecuting = false;
            }
        }
    }

    #endregion
}