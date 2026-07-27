using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class SceneController : MonoBehaviour
{
    // 버튼에 연결해서 사용할 함수입니다.
    public void ChangeScene(string sceneName)
    {
        // 시간을 다시 흐르게 합니다.
        Time.timeScale = 1f;

        // 플레이 씬에서 뒤로가기(StageSelect 등)를 누를 때, 쇼룸에서 출발했다면 쇼룸 씬으로 우회시킵니다.
        if (sceneName.Contains("StageSelect") || sceneName.Contains("Main"))
        {
            if (PlayerPrefs.GetInt("ReturnToShowroom", 0) == 1)
            {
                sceneName = "MuseumHub"; 
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    // 게임 종료 함수
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료!");
    }
}