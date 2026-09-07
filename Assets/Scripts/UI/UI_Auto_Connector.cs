using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject commandSequencePanel;
    public GameObject[] functionPanels; // F1, F2, F3 패널
    public GameObject commandSlotPrefab;
    public GameObject successPanel;
    public TextMeshProUGUI limitText;
    public GameObject limitBox;

    // 게임 중 UI 묶음
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
    public Button[] functionButtons; // 1, 2, 3 함수 버튼
    public Button ifButton;
    public Button whileButton;

    void Start()
    {
        var player = FindObjectOfType<PlayerController>();
        var camSwitcher = FindObjectOfType<CameraSwitcher>();
        var gameManager = FindObjectOfType<GameManager>();
        var sceneController = FindObjectOfType<SceneController>();
        var sm = SoundManager.Instance;

        // --- 0. 비플레이 씬(main, Intro, MuseumHub)인 경우 인게임 UI 비활성화 고정 ---
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isPlayScene = sceneName != "main" && sceneName != "Intro" && sceneName != "MuseumHub";
        
        if (!isPlayScene)
        {
            if (inGameUIGroup != null)
                inGameUIGroup.SetActive(false);
        }

        // --- 1. 플레이어 연결 ---
        // (중복 코드를 없애고 BindPlayer 함수를 호출합니다)
        if (player != null)
        {
            BindPlayer(player);
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
            gameManager.SetInGameUI(inGameUIGroup);

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
                    Time.timeScale = 1f; // 시간 정상화
                    sceneController.ChangeScene("Main");
                    sm?.PlayCommandClick();
                });
            }
        }
    }

    // 외부(MapEditor) 또는 Start에서 호출하여 플레이어와 UI를 연결하는 함수
    public void BindPlayer(PlayerController player)
    {
        if (player == null) return;

        var sm = SoundManager.Instance;
        var gameManager = FindObjectOfType<GameManager>();
        var camSwitcher = FindObjectOfType<CameraSwitcher>(); // (★ 추가: 카메라 매니저 찾기)

        // 1. 플레이어에게 StartBox 위치 강제 주입 (가장 중요!)
        // (플레이어가 스스로 못 찾을 경우를 대비해, 여기서 확실하게 다시 찾아서 넣어줌)
        if (player.startBox == null)
        {
            GameObject foundStart = GameObject.FindGameObjectWithTag("StartBox");
            if (foundStart != null) player.startBox = foundStart;
        }

        // 2. UI 초기화 및 전달
        // (이 함수 안에서 ResetPlayer가 호출되면서 위치가 잡힘)
        player.InitializeUI(
            commandSequencePanel,
            functionPanels,
            commandSlotPrefab,
            successPanel,
            limitText,
            inGameUIGroup,
            limitBox
        );

        // 3. (★ 핵심) 3인칭 카메라에게 "이 플레이어를 따라가!"라고 알려주기
        if (camSwitcher != null && camSwitcher.cameras.Length > 2)
        {
            // 3인칭 카메라(Element 2)를 찾아서
            Camera thirdCam = camSwitcher.cameras[2];
            // 그 부모나 본인에게 붙은 'ThirdPersonFollow' 스크립트를 찾음
            ThirdPersonFollow follower = thirdCam.GetComponentInParent<ThirdPersonFollow>();

            if (follower != null)
            {
                follower.target = player.transform; // 타겟을 새 플레이어로 교체!
            }
        }

        // 4. 버튼 리스너 연결 (기존 코드 유지)
        forwardButton.onClick.RemoveAllListeners();
        forwardButton.onClick.AddListener(() => { player.AddCommand_Forward(); });

        rightButton.onClick.RemoveAllListeners();
        rightButton.onClick.AddListener(() => { player.AddCommand_TurnRight(); });

        leftButton.onClick.RemoveAllListeners();
        leftButton.onClick.AddListener(() => { player.AddCommand_TurnLeft(); });

        if (ifButton != null)
        {
            ifButton.onClick.RemoveAllListeners();
            ifButton.onClick.AddListener(() => { player.AddCommand_If_Tree(); });
        }

        if (whileButton != null)
        {
            whileButton.onClick.RemoveAllListeners();
            whileButton.onClick.AddListener(() => { player.AddCommand_While_Tree(); });
        }

        executeButton.onClick.RemoveAllListeners();
        executeButton.onClick.AddListener(() => { player.ExecuteCommands(); sm?.PlayExecuteClick(); });

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            // 씬을 재시작하지 않고 플레이어의 위치와 커맨드 패널만 리셋합니다.
            resetButton.onClick.AddListener(() => { player.ResetGame(); sm?.PlayResetClick(); });
        }

        // --- 패널 배경 클릭 시 활성 창 변경 이벤트 바인딩 ---
        if (commandSequencePanel != null)
        {
            Button mainPanelBtn = commandSequencePanel.GetComponent<Button>();
            if (mainPanelBtn != null)
            {
                mainPanelBtn.onClick.RemoveAllListeners();
                mainPanelBtn.onClick.AddListener(() => player.SetActivePanel(-1));
            }
        }

        if (functionPanels != null)
        {
            for (int i = 0; i < functionPanels.Length; i++)
            {
                if (functionPanels[i] != null)
                {
                    int capturedIndex = i;
                    Button panelBtn = functionPanels[i].GetComponent<Button>();
                    if (panelBtn == null) panelBtn = functionPanels[i].gameObject.AddComponent<Button>();

                    if (panelBtn != null)
                    {
                        panelBtn.onClick.RemoveAllListeners();
                        panelBtn.onClick.AddListener(() => player.SetActivePanel(capturedIndex));
                    }
                }
            }
        }

        // --- 함수 버튼 1, 2, 3 바인딩 ---
        if (functionButtons != null)
        {
            for (int i = 0; i < functionButtons.Length; i++)
            {
                if (functionButtons[i] != null)
                {
                    int capturedIndex = i;
                    var handler = functionButtons[i].GetComponent<MouseButtonHandler>();
                    if (handler == null) handler = functionButtons[i].gameObject.AddComponent<MouseButtonHandler>();

                    handler.onLeftClick.RemoveAllListeners();
                    handler.onLeftClick.AddListener(() => 
                    {
                        Debug.Log($"[UI] 함수 버튼 {capturedIndex} 좌클릭 됨! (커맨드 추가 시도)");
                        player.AddCommand_CallFunction(capturedIndex);
                    });

                    handler.onRightClick.RemoveAllListeners();
                    handler.onRightClick.AddListener(() => 
                    {
                        Debug.Log($"[UI] 함수 버튼 {capturedIndex} 우클릭 됨! 패널 활성화 상태 토글 시도");
                        if (functionPanels != null && capturedIndex < functionPanels.Length && functionPanels[capturedIndex] != null)
                        {
                            bool isActive = functionPanels[capturedIndex].activeSelf;
                            functionPanels[capturedIndex].SetActive(!isActive);
                            Debug.Log($"[UI] 패널 {capturedIndex} 상태 변경: {!isActive}");
                            
                            // 패널이 켜지면 자동으로 그 패널을 활성화
                            if (!isActive) player.SetActivePanel(capturedIndex);
                            else player.SetActivePanel(-1); // 꺼지면 메인으로
                        }
                        else
                        {
                            Debug.LogError($"[UI] 에러: 함수 패널 {capturedIndex}번이 Inspector에 할당되지 않았습니다!");
                        }
                    });
                }
            }
        }
    }
}