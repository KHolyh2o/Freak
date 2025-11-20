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
        var sm = SoundManager.Instance; // 싱글톤 사용

        if (player != null)
        {
            player.InitializeUI(commandSequencePanel, loopSequencePanel, commandSlotPrefab, successPanel, loopConfigPopup, limitText);

            // 버튼 리스너 연결 (람다식으로 간결하게 표현)
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
                resetButton.onClick.AddListener(() => { player.ResetGame(); sm?.PlayResetClick(); });
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

        if (camSwitcher != null && cameraButton != null)
        {
            cameraButton.onClick.RemoveAllListeners();
            cameraButton.onClick.AddListener(() => { camSwitcher.SwitchCamera(); sm?.PlayCameraClick(); });
        }
    }
}