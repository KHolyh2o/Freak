using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("이 캔버스의 자식 UI")]
    public Text commandSequenceText;
    public GameObject successPanel;

    [Header("자동 연결할 버튼들")] // <-- (추가된 부분)
    public Button forwardButton;
    public Button rightButton;
    public Button leftButton;
    public Button executeButton;
    public Button resetButton;

    // ------------------------------------
    // 아래는 내부 로직
    // ------------------------------------

    private PlayerController playerController;

    void Start()
    {
        // 1. 씬에서 PlayerController를 찾습니다.
        playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            // 2. [기존] Player에게 UI (Text, Panel) 주입
            playerController.InitializeUI(commandSequenceText, successPanel);

            // 3. [★새로운 기능★] 버튼들에 Player의 함수를 연결
            // (인스펙터의 OnClick() 연결을 코드가 대신 자동으로 수행)

            // (중요) 인스펙터에서 실수로 연결했을 경우를 대비해, 기존 연결을 초기화
            forwardButton.onClick.RemoveAllListeners();
            // PlayerController의 함수를 리스너로 추가
            forwardButton.onClick.AddListener(playerController.AddCommand_Forward);

            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(playerController.AddCommand_TurnRight);

            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(playerController.AddCommand_TurnLeft);

            executeButton.onClick.RemoveAllListeners();
            executeButton.onClick.AddListener(playerController.ExecuteCommands);

            // "다시하기" 버튼이 SuccessPanel 안에 있든 밖에 있든 연결
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(playerController.ResetGame);
            }
        }
        else
        {
            Debug.LogError("UI_Auto_Connector가 씬에서 PlayerController를 찾지 못했습니다!");
        }
    }
}