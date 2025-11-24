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

    // (★ 추가된 변수) 게임 중 UI 묶음
    public GameObject inGameUIGroup;

    [Header("퍼즈 메뉴 UI")]
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
        var gameManager = FindObjectOfType<GameManager>();
        var sceneController = FindObjectOfType<SceneController>();
        var sm = SoundManager.Instance;

        // --- 1. 플레이어 연결 ---
        if (player != null)
        {
            // (★ 수정됨: inGameUIGroup을 7번째 인자로 전달)
            player.InitializeUI(
                commandSequencePanel,
                loopSequencePanel,
                commandSlotPrefab,
                successPanel,
                loopConfigPopup,
                limitText,
                inGameUIGroup // 여기!
            );

            // ... (버튼 연결 코드는 그대로) ...
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

        // --- 3. 퍼즈 시스템 연결 ---
        if (gameManager != null)
        {
            gameManager.SetPausePanel(pausePanel);

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() => { gameManager.PauseGame(); sm?.PlayCommandClick(); });
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(() => { gameManager.ResumeGame(); sm?.PlayCommandClick(); });
            }

            if (stageSelectButton != null && sceneController != null)
            {
                stageSelectButton.onClick.RemoveAllListeners();
                stageSelectButton.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f;
                    sceneController.ChangeScene("Main");
                    sm?.PlayCommandClick();
                });
            }
        }
    }
}