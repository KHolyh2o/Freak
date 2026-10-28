using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class GameManager : MonoBehaviour
{
    // UI_Auto_Connector가 연결해줄 퍼즈 패널
    private GameObject pausePanel;
    private GameObject inGameUIGroup;
    private bool isPaused = false;

    // UI_Auto_Connector가 호출해서 패널을 등록해주는 함수
    public void SetPausePanel(GameObject panel)
    {
        this.pausePanel = panel;
    }

    public void SetInGameUI(GameObject uiGroup)
    {
        this.inGameUIGroup = uiGroup;
    }

    void Update()
    {
        // 메뉴가 아닌 모든 씬(Play, Practice, Stage_00 등)에서 작동
        if (SceneIds.IsPlayScene(SceneFlow.Current))
        {
            if (GameInput.KeyDown(KeyCode.Escape))
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }
    }

    // --- 기능 함수들 ---

    // 일시 정지
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // ★ 시간을 멈춤 (물리, 애니메이션 정지)

        if (pausePanel != null)
            pausePanel.SetActive(true); // 패널 켜기
            
        if (inGameUIGroup != null)
            inGameUIGroup.SetActive(false);
    }

    // 게임 재개 (돌아가기)
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // ★ 시간을 다시 흐르게 함

        if (pausePanel != null)
            pausePanel.SetActive(false); // 패널 끄기
            
        if (inGameUIGroup != null)
            inGameUIGroup.SetActive(true);
    }

    // 현재 스테이지 재시작 (다시하기 버튼용)
    public void RestartLevel()
    {
        SceneFlow.Restart();
    }

    // 클리어 패널의 "다음" 버튼용. 갈 곳은 SceneFlow.Next가 현재 씬에 맞춰 정한다.
    public void NextStage()
    {
        SceneFlow.Next();
    }

    // 메인(스테이지 선택) 화면으로 이동
    public void GoToMain()
    {
        SceneFlow.ToMain();
    }
}