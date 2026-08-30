using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PanelTabController : MonoBehaviour
{
    [Header("타겟 패널 설정")]
    [Tooltip("이 탭을 눌렀을 때 맨 앞으로 가져올(활성화할) 패널 오브젝트")]
    public GameObject targetPanel;

    [Tooltip("이 패널의 인덱스 (메인 패널: -1, F1: 0, F2: 1, F3: 2)")]
    public int panelIndex;

    private Button tabButton;

    private void Awake()
    {
        tabButton = GetComponent<Button>();
        if (tabButton != null)
        {
            tabButton.onClick.AddListener(OnTabClicked);
        }
    }

    private void OnTabClicked()
    {
        if (targetPanel != null)
        {
            // 1. 타겟 패널을 부모(Canvas 등) 계층에서 가장 아래(화면 맨 앞)로 이동
            targetPanel.transform.SetAsLastSibling();
        }

        // 2. PlayerController에 해당 패널을 활성화 상태로 변경하도록 알림
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetActivePanel(panelIndex);
            SoundManager.Instance?.PlayCommandClick();
        }
    }
}
