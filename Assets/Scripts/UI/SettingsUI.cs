using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    
    [Header("UI Elements")]
    public Slider masterVolumeSlider;
    
    [Header("Screen Mode Toggle (Button)")]
    public Button screenModeButton;
    public Image screenModeImage; 
    public Sprite fullScreenSprite;
    public Sprite windowedSprite;

    [Header("AI Support Toggle")]
    public Button aiSupportButton;
    public Image aiSupportImage;
    public Sprite aiSupportOnSprite;
    public Sprite aiSupportOffSprite;

    [Header("Dummy Toggle")]
    public Button dummyToggleButton;
    public Image dummyToggleImage;
    public Sprite dummyOffSprite; // 더미 기능이므로 OFF만 사용한다고 가정

    [Header("Language Dropdown")]
    public Button languageDropdownButton; // 나중에 확장 시 사용. 현재는 비활성화.
    
    [Header("Action Buttons")]
    public Button confirmButton;
    public Button mainButton;

    private void Start()
    {
        // 1. 값 동기화
        if (SettingsManager.Instance != null)
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = SettingsManager.Instance.masterVolume;
                masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            
            UpdateScreenModeUI();
            UpdateAISupportUI();
        }

        // 2. 이벤트 리스너 등록
        if (screenModeButton != null)
            screenModeButton.onClick.AddListener(OnScreenModeToggleClicked);

        if (aiSupportButton != null)
            aiSupportButton.onClick.AddListener(OnAISupportToggleClicked);

        // 더미 버튼은 클릭 시 아무 기능도 없지만 시각적 효과를 원한다면 구현할 수 있습니다.
        // 현재는 항상 OFF 상태 유지.
        
        // 언어 버튼 시각적 비활성화 (영어 선택 불가능)
        if (languageDropdownButton != null)
        {
            languageDropdownButton.interactable = false; // 클릭 비활성화
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            AddHoverEffect(confirmButton.gameObject);
        }

        if (mainButton != null)
        {
            mainButton.onClick.AddListener(OnMainButtonClicked);
            AddHoverEffect(mainButton.gameObject);
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnScreenModeToggleClicked()
    {
        if (SettingsManager.Instance != null)
        {
            bool isFull = !SettingsManager.Instance.isFullScreen;
            SettingsManager.Instance.SetFullScreen(isFull);
            UpdateScreenModeUI();
            SoundManager.Instance?.PlayCommandClick();
        }
    }

    private void UpdateScreenModeUI()
    {
        if (SettingsManager.Instance != null && screenModeImage != null)
        {
            screenModeImage.sprite = SettingsManager.Instance.isFullScreen ? fullScreenSprite : windowedSprite;
        }
    }

    private void OnAISupportToggleClicked()
    {
        if (SettingsManager.Instance != null)
        {
            bool isEnabled = !SettingsManager.Instance.isAISupportEnabled;
            SettingsManager.Instance.SetAISupport(isEnabled);
            UpdateAISupportUI();
            SoundManager.Instance?.PlayCommandClick();
        }
    }

    private void UpdateAISupportUI()
    {
        if (SettingsManager.Instance != null && aiSupportImage != null)
        {
            aiSupportImage.sprite = SettingsManager.Instance.isAISupportEnabled ? aiSupportOnSprite : aiSupportOffSprite;
        }
    }

    private void OnConfirmClicked()
    {
        SoundManager.Instance?.PlayCommandClick();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void OnMainButtonClicked()
    {
        SoundManager.Instance?.PlayCommandClick();
        // 메인 씬으로 돌아가는 로직
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene"); 
    }

    // 마우스를 올렸을 때 버튼이 눌릴 것임을 표시하기 위한 호버 이펙트 (약간 어두워지거나 크기 변경)
    private void AddHoverEffect(GameObject obj)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        // PointerEnter (마우스 올림)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            // 크기를 살짝 키워서 피드백 주기
            obj.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
        });
        trigger.triggers.Add(enterEntry);

        // PointerExit (마우스 빠져나감)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            obj.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(exitEntry);
        
        // PointerDown (마우스 클릭 중)
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => {
            obj.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
        });
        trigger.triggers.Add(downEntry);
        
        // PointerUp (마우스 클릭 뗌)
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => {
            obj.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f); // 여전히 올려져 있으면 커진 상태 유지
        });
        trigger.triggers.Add(upEntry);
    }
}
