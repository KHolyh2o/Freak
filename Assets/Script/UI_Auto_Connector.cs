using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject commandSequencePanel;
    public GameObject loopSequencePanel;
    public GameObject commandSlotPrefab;
    public GameObject successPanel;
    public GameObject loopConfigPopup;
    public TextMeshProUGUI limitText;

    [Header("퍼즈 메뉴 UI")] // (★ 복구됨)
    public GameObject pausePanel;
    public Button settingsButton;
    public Button resumeButton;
    public Button stageSelectButton;

    [Header("버튼 연결")]
    public Button forwardButton;
    public Button rightButton;
    public Button leftButton;
    public Button executeButton;
    public Button resetButton;
    public Button cameraButton;
    public Button loopButton;

    void Start()
    {
        var player = FindObjectOfType<PlayerController>();
        var camSwitcher = FindObjectOfType<CameraSwitcher>();
        var gameManager = FindObjectOfType<GameManager>(); // (★ 복구됨)
        var sceneController = FindObjectOfType<SceneController>(); // (★ 복구됨)
        var sm = SoundManager.Instance;

        // --- 1. 플레이어 연결 ---
        if (player != null)
        {
            player.InitializeUI(commandSequencePanel, loopSequencePanel, commandSlotPrefab, successPanel, loopConfigPopup, limitText);

            forwardButton.onClick.RemoveAllListeners();
            forwardButton.onClick.AddListener(() => { player.AddCommand_Forward(); });

            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(() => { player.AddCommand_TurnRight(); });

            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(() => { player.AddCommand_TurnLeft(); });

            executeButton.onClick.RemoveAllListeners();
            executeButton.onClick.AddListener(() => { player.ExecuteCommands(); sm?.PlayExecuteClick(); });

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                // 리셋 버튼은 GameManager의 재시작 기능을 쓰는 게 더 안전함 (퍼즈 해제 등을 위해)
                if (gameManager != null)
                {
                    resetButton.onClick.AddListener(() => { gameManager.RestartLevel(); sm?.PlayResetClick(); });
                }
                else
                {
                    resetButton.onClick.AddListener(() => { player.ResetGame(); sm?.PlayResetClick(); });
                }
            }

            if (loopButton != null)
            {
                var handler = loopButton.GetComponent<MouseButtonHandler>();
                if (handler == null) handler = loopButton.gameObject.AddComponent<MouseButtonHandler>();

                handler.onLeftClick.RemoveAllListeners();
                handler.onLeftClick.AddListener(player.AddCommand_Loop_ToMain);

                handler.onRightClick.RemoveAllListeners();
                handler.onRightClick.AddListener(player.OpenLoopConfigPopup);
            }
        }

        // --- 2. 카메라 연결 ---
        if (camSwitcher != null && cameraButton != null)
        {
            cameraButton.onClick.RemoveAllListeners();
            cameraButton.onClick.AddListener(() => { camSwitcher.SwitchCamera(); sm?.PlayCameraClick(); });
        }

        // --- 3. 퍼즈 시스템 연결 (★ 복구됨) ---
        if (gameManager != null)
        {
            gameManager.SetPausePanel(pausePanel);

            // 설정 버튼 -> 일시정지
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() => { gameManager.PauseGame(); sm?.PlayCommandClick(); });
            }

            // 돌아가기 버튼 -> 재개
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(() => { gameManager.ResumeGame(); sm?.PlayCommandClick(); });
            }

            // 스테이지 선택 -> 씬 이동 (SceneController 사용)
            if (stageSelectButton != null && sceneController != null)
            {
                stageSelectButton.onClick.RemoveAllListeners();
                stageSelectButton.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f; // 시간 정상화
                    sceneController.ChangeScene("Main");
                    sm?.PlayCommandClick();
                });
            }
        }
    }
}