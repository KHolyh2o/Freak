using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "main"; // 영상이 끝나면 넘어갈 씬 이름

    void Start()
    {
        // VideoPlayer 컴포넌트가 연결되어 있지 않다면 자기 자신에게서 찾습니다.
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            // 영상 재생이 끝났을 때(loopPointReached) OnVideoEnd 함수를 실행하도록 등록합니다.
            videoPlayer.loopPointReached += OnVideoEnd;
        }
        else
        {
            Debug.LogWarning("VideoPlayer 컴포넌트가 없습니다! 바로 다음 씬으로 이동합니다.");
            LoadNextScene();
        }
    }

    void Update()
    {
        // (선택 기능) 마우스를 클릭하거나 아무 키나 누르면 영상을 스킵하고 넘어갑니다.
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // 영상 재생이 끝났을 때 호출됩니다.
        LoadNextScene();
    }

    void LoadNextScene()
    {
        // 이벤트 연결을 해제하고 다음 씬으로 넘어갑니다.
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
            
        SceneManager.LoadScene(nextSceneName);
    }
}
