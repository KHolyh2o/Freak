using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("UI 사운드")]
    public AudioClip commandButtonSound;
    public AudioClip executeButtonSound;
    public AudioClip resetButtonSound;
    public AudioClip cameraButtonSound;
    public AudioClip loopButtonSound;

    [Header("게임 플레이 사운드")]
    public AudioClip stepSound;
    public AudioClip fallSound;
    public AudioClip successSound;
    public AudioClip bumpSound;
    public AudioClip puzzleSound;

    [Header("배경 음악")]
    public AudioClip bgmClip;

    private void Awake()
    {
        // --- 싱글톤 & 파괴 방지 패턴 ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ★ 핵심: 씬이 바뀌어도 나를 파괴하지 마라!
        }
        else
        {
            // 이미 살아있는 원조 SoundManager가 있다면, 
            // 새로 생긴 나는(중복) 필요 없으니 사라진다.
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 게임 시작 시 BGM 재생 요청
        PlayBGM(bgmClip);
    }

    // --- BGM 재생 함수 (똑똑한 버전) ---
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        // ★ 핵심: 만약 지금 재생 중인 곡이 요청한 곡과 똑같다면?
        // 아무것도 하지 말고 계속 틀어둔다. (끊김 방지)
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 다른 곡이라면 교체하고 재생한다.
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // --- 효과음 함수들 (기존과 동일) ---
    public void PlayCommandClick() => PlaySFX(commandButtonSound);
    public void PlayExecuteClick() => PlaySFX(executeButtonSound);
    public void PlayResetClick() => PlaySFX(resetButtonSound);
    public void PlayCameraClick() => PlaySFX(cameraButtonSound);
    public void PlayLoopClick() => PlaySFX(loopButtonSound);

    public void PlayStep() => PlaySFX(stepSound);
    public void PlayFall() => PlaySFX(fallSound);
    public void PlaySuccess() => PlaySFX(successSound);
    public void PlayBump() => PlaySFX(bumpSound);
    public void PlayPuzzle() => PlaySFX(puzzleSound);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
}