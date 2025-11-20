using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 어디서든 SoundManager.Instance 로 접근 가능하게 만듦
    public static SoundManager Instance;

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("UI 사운드")]
    public AudioClip commandButtonSound;
    public AudioClip executeButtonSound;
    public AudioClip resetButtonSound;
    public AudioClip cameraButtonSound;

    [Header("게임 플레이 사운드")]
    public AudioClip stepSound;
    public AudioClip fallSound;
    public AudioClip successSound;
    public AudioClip bumpSound;

    [Header("배경 음악")]
    public AudioClip bgmClip;

    private void Awake()
    {
        // 싱글톤 설정 (중복 방지)
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // --- 간결해진 재생 함수들 ---
    public void PlayCommandClick() => PlaySFX(commandButtonSound);
    public void PlayExecuteClick() => PlaySFX(executeButtonSound);
    public void PlayResetClick() => PlaySFX(resetButtonSound);
    public void PlayCameraClick() => PlaySFX(cameraButtonSound);

    public void PlayStep() => PlaySFX(stepSound);
    public void PlayFall() => PlaySFX(fallSound);
    public void PlaySuccess() => PlaySFX(successSound);
    public void PlayBump() => PlaySFX(bumpSound);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
}