using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    public GameObject settingsPanel; // 인스펙터에서 Settings_Canvas(또는 패널)를 연결하세요.

    public void OnStartClicked()
    {
        // 1. 시작하기 버튼: 메인 씬(맵 선택)으로 이동
        SoundManager.Instance?.PlayCommandClick();
        Debug.Log("[StartSceneUI] 시작하기 클릭 -> main 씬으로 이동");
        SceneManager.LoadScene("main");
    }

    public void OnSettingsClicked()
    {
        // 2. 설정 버튼: 설정창 켜기
        SoundManager.Instance?.PlayCommandClick();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[StartSceneUI] Settings Panel이 인스펙터에 연결되지 않았습니다!");
        }
    }

    public void OnQuitClicked()
    {
        // 3. 게임 종료 버튼: 게임 완전히 끄기
        SoundManager.Instance?.PlayCommandClick();
        Debug.Log("[StartSceneUI] 게임 종료 요청");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
