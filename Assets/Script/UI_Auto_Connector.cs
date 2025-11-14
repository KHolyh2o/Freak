using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Auto_Connector : MonoBehaviour
{
    [Header("이 캔버스의 자식 UI")]
    // (수정됨) Text 대신 Panel과 Slot Prefab을 연결합니다.
    public GameObject commandSequencePanel;
    public GameObject commandSlotPrefab;
    public GameObject successPanel;

    [Header("자동 연결할 버튼들")]
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
        playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            // --- (★ 여기가 수정된 부분입니다) ---
            // 3개의 인자(cmdPanel, cmdSlotPfb, successPnl)를 모두 전달합니다.
            playerController.InitializeUI(commandSequencePanel, commandSlotPrefab, successPanel);

            // ------------------- 버튼 연결 로직 (변경 없음) -------------------
            forwardButton.onClick.RemoveAllListeners();
            forwardButton.onClick.AddListener(playerController.AddCommand_Forward);

            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(playerController.AddCommand_TurnRight);

            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(playerController.AddCommand_TurnLeft);

            executeButton.onClick.RemoveAllListeners();
            executeButton.onClick.AddListener(playerController.ExecuteCommands);

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