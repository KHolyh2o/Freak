using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "main"; // 영상이 끝나면 넘어갈 씬 이름
    
    [Header("설정")]
    [Tooltip("영상이 시작되기 전 대기하는 시간(초)")]
    public float delayBeforePlay = 0.5f;

    void Start()
    {
        // VideoPlayer 컴포넌트가 연결되어 있지 않다면 자기 자신에게서 찾습니다.
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            // 맥(16:10) 등 비율이 다른 모니터에서도 16:9 영상 비율이 찌그러지지 않고 유지되도록 설정 (FitInside)
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            
            // 영상 위아래에 남는 공간(레터박스)으로 뒤의 스카이박스가 보이지 않도록 메인 카메라 배경을 검은색으로 덮습니다.
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }

            // 영상 재생이 끝났을 때(loopPointReached) OnVideoEnd 함수를 실행하도록 등록합니다.
            videoPlayer.loopPointReached += OnVideoEnd;
            
            // 지정된 시간(delayBeforePlay) 뒤에 영상 재생을 시작합니다.
            Invoke("PlayVideo", delayBeforePlay);
        }
        else
        {
            Debug.LogWarning("VideoPlayer 컴포넌트가 없습니다! 바로 다음 씬으로 이동합니다.");
            LoadNextScene();
        }
    }
    
    void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
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
        // 혹시 대기 중에 스킵했을 경우를 대비해 예약된 재생 취소
        CancelInvoke("PlayVideo");
        
        // 이벤트 연결을 해제하고 다음 씬으로 넘어갑니다.
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
            
        SceneManager.LoadScene(nextSceneName);
    }
}

