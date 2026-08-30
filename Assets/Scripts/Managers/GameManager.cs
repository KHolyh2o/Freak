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
        // ESC 키 입력 감지 -> 퍼즈 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
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
        // 재시작 전에 반드시 시간을 정상화해야 함
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}