using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // (★ 필수)

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("이 캔버스의 자식 UI")]
    public GameObject commandSequencePanel;
    public GameObject loopSequencePanel;
    public GameObject commandSlotPrefab;
    public GameObject successPanel;
    public GameObject loopConfigPopup;

    public TextMeshProUGUI limitText; // (★ TMP 변경)

    [Header("자동 연결할 버튼들")]
    public Button forwardButton;
    public Button rightButton;
    public Button leftButton;
    public Button executeButton;
    public Button resetButton;
    public Button cameraButton;
    public Button loopButton;

    private PlayerController playerController;
    private CameraSwitcher cameraSwitcher;
    private SoundManager soundManager;

    void Start()
    {
        soundManager = FindObjectOfType<SoundManager>();
        playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            // (★수정됨: 인자 6개 전달)
            playerController.InitializeUI(
                commandSequencePanel,
                loopSequencePanel,
                commandSlotPrefab,
                successPanel,
                loopConfigPopup,
                limitText
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

            if (loopButton != null)
            {
                MouseButtonHandler handler = loopButton.GetComponent<MouseButtonHandler>();
                if (handler == null) handler = loopButton.gameObject.AddComponent<MouseButtonHandler>();

                handler.onLeftClick.RemoveAllListeners();
                handler.onLeftClick.AddListener(playerController.AddCommand_Loop_ToMain);

                handler.onRightClick.RemoveAllListeners();
                handler.onRightClick.AddListener(playerController.OpenLoopConfigPopup);
            }
        }
        else
        {
            Debug.LogError("UI_Auto_Connector가 씬에서 PlayerController를 찾지 못했습니다!");
        }

        // --- 카메라 연결 ---
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