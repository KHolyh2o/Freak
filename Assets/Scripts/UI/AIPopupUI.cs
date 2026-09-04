using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AIPopupUI : MonoBehaviour
{
    public static AIPopupUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject popupPanel;
    public TextMeshProUGUI messageText;
    public Button practiceButton;
    public Button closeButton;

    [Header("Loading Animation")]
    public Image loadingImage;
    public Sprite[] loadingSprites;
    public float loadingAnimSpeed = 0.5f;
    private Coroutine loadingAnimCoroutine;

    private string currentMissingSkill;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (practiceButton != null)
            practiceButton.onClick.AddListener(OnPracticeClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    public void ShowLoading()
    {
        // 로딩 중에는 패널 배경을 숨깁니다.
        if (popupPanel != null)
            popupPanel.SetActive(false);
            
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.ToggleUIVisibility(false);
        
        if (practiceButton != null) practiceButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        // 텍스트를 숨기고 로딩 이미지를 보여주며 애니메이션 시작
        if (messageText != null) messageText.gameObject.SetActive(false);
        if (loadingImage != null)
        {
            loadingImage.gameObject.SetActive(true);
            if (loadingAnimCoroutine != null) StopCoroutine(loadingAnimCoroutine);
            loadingAnimCoroutine = StartCoroutine(AnimateLoadingSprites());
        }
    }

    private IEnumerator AnimateLoadingSprites()
    {
        if (loadingSprites == null || loadingSprites.Length == 0) yield break;
        int index = 0;
        while (true)
        {
            if (loadingImage != null) loadingImage.sprite = loadingSprites[index];
            index = (index + 1) % loadingSprites.Length;
            yield return new WaitForSeconds(loadingAnimSpeed);
        }
    }

    public void ShowMessage(string message, string missingSkill)
    {
        currentMissingSkill = missingSkill;

        if (popupPanel != null)
            popupPanel.SetActive(true);
            
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.ToggleUIVisibility(false);
        
        if (practiceButton != null) practiceButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        // 로딩 애니메이션 중지 및 숨기기
        if (loadingAnimCoroutine != null) StopCoroutine(loadingAnimCoroutine);
        if (loadingImage != null) loadingImage.gameObject.SetActive(false);
        
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(message));
        }
    }

    private IEnumerator TypeText(string message)
    {
        messageText.text = "";
        foreach (char c in message.ToCharArray())
        {
            messageText.text += c;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnPracticeClicked()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
            
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.ToggleUIVisibility(true);

        if (PracticeMapGenerator.Instance != null)
        {
            PracticeMapGenerator.Instance.GenerateAndSavePracticeMap(currentMissingSkill);
        }
        else
        {
            Debug.LogError("[AIPopupUI] PracticeMapGenerator를 찾을 수 없습니다.");
        }
    }

    private void OnCloseClicked()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
            
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.ToggleUIVisibility(true);
            
        // 거절했으므로 카운트를 리셋하여 다시 시도할 수 있게 해줌
        if (AIManager.Instance != null)
        {
            AIManager.Instance.ResetStageData();
        }
    }
}
