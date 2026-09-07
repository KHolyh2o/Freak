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
        string sceneName = SceneManager.GetActiveScene().name;
        // main, Intro, MuseumHub 등 비플레이 씬을 제외한 모든 플레이 씬(Practice, Stage_00 등)에서 작동하도록 수정
        bool isPlayScene = sceneName != "main" && sceneName != "Intro" && sceneName != "MuseumHub";

        if (isPlayScene)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
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
        // 재시작 전에 반드시 시간을 정상화해야 함
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 다음 스테이지로 이동 (다음 스테이지 버튼용)
    public void NextStage()
    {
        Time.timeScale = 1f;
        // 현재 스테이지 인덱스를 가져와서 1 증가 (0이 Stage 1)
        int currentStage = PlayerPrefs.GetInt("SelectedStage", 0);
        PlayerPrefs.SetInt("SelectedStage", currentStage + 1);
        PlayerPrefs.Save();
        
        // 현재 Play 씬을 다시 로드하면 TestMapLoader가 새로 바뀐 SelectedStage 값을 읽어 다음 맵을 생성함
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 메인(또는 스테이지 선택) 화면으로 이동
    public void GoToMain()
    {
        Time.timeScale = 1f;
        
        // 쇼룸에서 들어온 경우 다시 쇼룸으로 돌려보내는 로직 (SceneController와 동일)
        if (PlayerPrefs.GetInt("ReturnToShowroom", 0) == 1)
        {
            SceneManager.LoadScene("MuseumHub"); 
        }
        else
        {
            // 실제 씬 이름인 "main"으로 변경
            SceneManager.LoadScene("main"); 
        }
    }
}