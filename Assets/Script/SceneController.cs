using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class SceneController : MonoBehaviour
{
    // 버튼에 연결해서 사용할 함수입니다.
    // 인스펙터에서 이동할 씬 이름을 직접 적어줄 수 있습니다.
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // (선택 사항) 게임 종료 함수
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료!"); // 에디터에서는 안 꺼지므로 로그로 확인
    }
}