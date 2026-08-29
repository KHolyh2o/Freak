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

    public void ApplySettings()
    {
        // 1. 볼륨 적용
        AudioListener.volume = masterVolume;

        // 2. 화면 모드 적용
        if (isFullScreen)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // 창 모드 기본 해상도 (1920x1080)
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
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
