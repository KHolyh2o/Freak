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
        if (popupPanel != null)
            popupPanel.SetActive(true);
        
        if (practiceButton != null) practiceButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        messageText.text = "AI 어시스턴트가 분석 중입니다...";
    }

    public void ShowMessage(string message, string missingSkill)
    {
        currentMissingSkill = missingSkill;

        if (popupPanel != null)
            popupPanel.SetActive(true);
        
        if (practiceButton != null) practiceButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(message));
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
            
        // 거절했으므로 카운트를 리셋하여 다시 시도할 수 있게 해줌
        if (AIManager.Instance != null)
        {
            AIManager.Instance.ResetStageData();
        }
    }
}
