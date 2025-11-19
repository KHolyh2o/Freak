using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("이 캔버스의 자식 UI")]
    public GameObject commandSequencePanel; // 메인 시퀀스 패널 (Content)
    public GameObject loopSequencePanel;    // (★추가) 반복 설정창의 시퀀스 패널 (Content)

    public GameObject commandSlotPrefab;    // 아이콘 프리팹
    public GameObject successPanel;         // 성공 패널
    public GameObject loopConfigPopup;      // (★추가) 반복 설정 팝업창 전체

    [Header("자동 연결할 버튼들")]
    public Button forwardButton;
    public Button rightButton;
    public Button leftButton;
    public Button executeButton;
    public Button resetButton;
    public Button cameraButton;

    public Button loopButton; // (★추가) Loop 버튼

    // ------------------------------------
    // 아래는 내부 로직
    // ------------------------------------

    private PlayerController playerController;
    private CameraSwitcher cameraSwitcher;
    private SoundManager soundManager;

    void Start()
    {
        soundManager = FindObjectOfType<SoundManager>();

        // --- 1. 플레이어 찾기 및 연결 ---
        playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            // (★수정됨) 5개의 인자를 순서대로 전달합니다.
            // (메인패널, 루프패널, 프리팹, 성공패널, 팝업창)
            playerController.InitializeUI(
                commandSequencePanel,
                loopSequencePanel,
                commandSlotPrefab,
                successPanel,
                loopConfigPopup
            );

            // --- 버튼 연결 ---
            forwardButton.onClick.RemoveAllListeners();
            forwardButton.onClick.AddListener(playerController.AddCommand_Forward);
            forwardButton.onClick.AddListener(() => soundManager?.PlayCommandClick());

            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(playerController.AddCommand_TurnRight);
            rightButton.onClick.AddListener(() => soundManager?.PlayCommandClick());

            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(playerController.AddCommand_TurnLeft);
            leftButton.onClick.AddListener(() => soundManager?.PlayCommandClick());

            executeButton.onClick.RemoveAllListeners();
            executeButton.onClick.AddListener(playerController.ExecuteCommands);
            executeButton.onClick.AddListener(() => soundManager?.PlayExecuteClick());

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(playerController.ResetGame);
                resetButton.onClick.AddListener(() => soundManager?.PlayResetClick());
            }

            // (★추가) Loop 버튼 연결 (좌클릭은 Main에 추가, 우클릭은 팝업 열기)
            if (loopButton != null)
            {
                // 1. 마우스 핸들러 가져오기 (없으면 추가)
                MouseButtonHandler mouseHandler = loopButton.GetComponent<MouseButtonHandler>();
                if (mouseHandler == null) mouseHandler = loopButton.gameObject.AddComponent<MouseButtonHandler>();

                // 2. 이벤트 연결
                mouseHandler.onLeftClick.RemoveAllListeners();
                mouseHandler.onLeftClick.AddListener(playerController.AddCommand_Loop_ToMain);
                // mouseHandler.onLeftClick.AddListener(() => soundManager?.PlayCommandClick()); // 필요시 추가

                mouseHandler.onRightClick.RemoveAllListeners();
                mouseHandler.onRightClick.AddListener(playerController.OpenLoopConfigPopup);
            }
        }
        else
        {
            Debug.LogError("UI_Auto_Connector가 씬에서 PlayerController를 찾지 못했습니다!");
        }

        // --- 2. 카메라 연결 ---
        cameraSwitcher = FindObjectOfType<CameraSwitcher>();
        if (cameraSwitcher != null)
        {
            if (cameraButton != null)
            {
                cameraButton.onClick.RemoveAllListeners();
                cameraButton.onClick.AddListener(cameraSwitcher.SwitchCamera);
                cameraButton.onClick.AddListener(() => soundManager?.PlayCameraClick());
            }
        }
    }
}