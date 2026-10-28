using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class SceneController : MonoBehaviour
{
    // 버튼에 연결해서 사용할 함수입니다.
    public void ChangeScene(string sceneName)
    {
        SceneFlow.Load(sceneName);
    }

    // 게임 종료 함수
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료!");
    }
}