using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings Data")]
    public float masterVolume = 1f;
    public bool isFullScreen = true;
    public bool isAISupportEnabled = true;

    private const string PREF_VOLUME = "Settings_MasterVolume";
    private const string PREF_SCREENMODE = "Settings_FullScreen";
    private const string PREF_AISUPPORT = "Settings_AISupport";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            ApplySettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 1f);
        isFullScreen = PlayerPrefs.GetInt(PREF_SCREENMODE, 1) == 1;
        isAISupportEnabled = PlayerPrefs.GetInt(PREF_AISUPPORT, 1) == 1;
    }

    private void Update()
    {
        // 글로벌 ESC 키 감지 (Play 씬 제외 - Play 씬은 GameManager가 담당)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "Play")
            {
                // 꺼져 있는 UI까지 찾아옴
                SettingsUI[] uis = Resources.FindObjectsOfTypeAll<SettingsUI>();
                foreach (var ui in uis)
                {
                    // 에셋(프리팹)이 아닌 씬에 존재하는 객체만 조작
                    if (ui.gameObject.scene.isLoaded)
                    {
                        bool isActive = ui.gameObject.activeSelf;
                        ui.gameObject.SetActive(!isActive);

                        if (!isActive)
                        {
                            SoundManager.Instance?.PlayCommandClick();
                            ui.UpdateButtonVisibility();
                        }
                        break; // 하나만 조작하고 종료
                    }
                }
            }
        }
    }

    public void ApplySettings()
    {
        // 1. 볼륨 적용
        AudioListener.volume = masterVolume;

        // 2. 화면 모드 적용
        if (isFullScreen)
        {
            // 강제로 1920x1080 (16:9) 해상도의 전용 전체화면 모드로 설정
            Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen);
        }
        else
        {
            // 창 모드일 때는 화면에 쏙 들어오는 작은 16:9 비율 (1280x720)로 설정
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_VOLUME, masterVolume);
        AudioListener.volume = masterVolume;
    }

    public void SetFullScreen(bool fullScreen)
    {
        isFullScreen = fullScreen;
        PlayerPrefs.SetInt(PREF_SCREENMODE, isFullScreen ? 1 : 0);
        ApplySettings();
    }

    public void SetAISupport(bool enabled)
    {
        isAISupportEnabled = enabled;
        PlayerPrefs.SetInt(PREF_AISUPPORT, isAISupportEnabled ? 1 : 0);
    }
}
