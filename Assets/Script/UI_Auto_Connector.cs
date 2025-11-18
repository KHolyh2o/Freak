using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("이 캔버스의 자식 UI")]
    public GameObject commandSequencePanel;
    public GameObject commandSlotPrefab;
    public GameObject successPanel;

    [Header("자동 연결할 버튼들")]
    public Button forwardButton;
    public Button rightButton;
    public Button leftButton;
    public Button executeButton;
    public Button resetButton;

    public Button cameraButton; // <-- (★ 1. 새 카메라 버튼 변수 추가)

    // ------------------------------------
    // 아래는 내부 로직
    // ------------------------------------

    private PlayerController playerController;
    private CameraSwitcher cameraSwitcher; // <-- (★ 2. 카메라 스위처 변수 추가)

    private SoundManager soundManager;

    void Start()
    {
        soundManager = FindObjectOfType<SoundManager>();
        // --- 1. 플레이어 찾기 및 연결 ---
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.InitializeUI(commandSequencePanel, commandSlotPrefab, successPanel);

            // 버튼 연결
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
        }
        else
        {
            Debug.LogError("UI_Auto_Connector가 씬에서 PlayerController를 찾지 못했습니다!");
        }

        // --- (★ 3. 카메라 스위처 찾기 및 버튼 연결 ★) ---
        cameraSwitcher = FindObjectOfType<CameraSwitcher>();
        if (cameraSwitcher != null)
        {
            if (cameraButton != null)
            {
                cameraButton.onClick.RemoveAllListeners();
                cameraButton.onClick.AddListener(cameraSwitcher.SwitchCamera);
                cameraButton.onClick.AddListener(() => soundManager?.PlayCameraClick());
            }
            else
            {
                Debug.LogWarning("UI_Auto_Connector: 'cameraButton' 슬롯이 연결되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogError("UI_Auto_Connector가 씬에서 CameraSwitcher를 찾지 못했습니다!");
        }
    }
}