using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class SceneController : MonoBehaviour
{
    // 버튼에 연결해서 사용할 함수입니다.
    public void ChangeScene(string sceneName)
    {
        // ★ (중요) 씬을 이동하기 전에 멈춘 시간을 다시 흐르게 만듭니다.
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }

    // 게임 종료 함수
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료!");
    }
}