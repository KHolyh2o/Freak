using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("오디오 소스 (스피커)")]
    public AudioSource bgmSource; // 배경음악용 (Loop 체크)
    public AudioSource sfxSource; // 효과음용

    [Header("UI 사운드")]
    public AudioClip commandButtonSound; // 전진, 회전 버튼
    public AudioClip executeButtonSound; // 실행 버튼
    public AudioClip resetButtonSound;   // 다시하기 버튼
    public AudioClip cameraButtonSound;  // 카메라 전환 버튼

    [Header("게임 플레이 사운드")]
    public AudioClip stepSound;    // 커맨드 실행될 때마다 (한 칸 이동 시)
    public AudioClip fallSound;    // 떨어졌을 때
    public AudioClip successSound; // 성공했을 때
    public AudioClip bumpSound;    // 장애물 충돌 소리

    [Header("배경 음악")]
    public AudioClip bgmClip;

    void Start()
    {
        // 배경음악 재생
        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true; // 무한 반복
            bgmSource.Play();
        }
    }

    // --- 외부에서 호출할 함수들 ---

    public void PlayCommandClick() { PlaySFX(commandButtonSound); }
    public void PlayExecuteClick() { PlaySFX(executeButtonSound); }
    public void PlayResetClick() { PlaySFX(resetButtonSound); }
    public void PlayCameraClick() { PlaySFX(cameraButtonSound); }
    public void PlayBump() { PlaySFX(bumpSound); }

    public void PlayStep() { PlaySFX(stepSound); }
    public void PlayFall() { PlaySFX(fallSound); }
    public void PlaySuccess() { PlaySFX(successSound); }

    // 효과음 재생 도우미 함수
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip); // PlayOneShot은 소리가 겹쳐도 끊기지 않음
        }
    }
}