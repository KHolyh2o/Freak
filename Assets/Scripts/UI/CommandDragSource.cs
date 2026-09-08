using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommandDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerController.CommandType commandType;
    public int callFunctionIndex = -1;
    public PlayerController.ObstacleType conditionObstacle = PlayerController.ObstacleType.None;

    [Header("해금(Unlock) 설정")]
    public int requiredUnlockStage = 1; // 이 커맨드가 해금되는 스테이지 번호 (기본 1)
    public Sprite lockIconSprite;       // 자물쇠 아이콘 이미지
    public bool useDebugUnlockStage = false; // 에디터 테스트용 강제 해금 레벨 사용 여부
    public int debugUnlockStage = 1;         // 에디터 테스트용 해금 레벨
    
    [Tooltip("이 커맨드가 잠겨있을 때 비활성화(SetActive(false))할 하위 오브젝트들")]
    public GameObject[] objectsToDisableWhenLocked;

    private GameObject ghostObj;
    private RectTransform ghostRect;
    private PlayerController playerController;
    private bool isLocked = false;
    
    public bool IsLocked => isLocked;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        CheckLockStatus();
    }

    private void CheckLockStatus()
    {
        int currentUnlocked = useDebugUnlockStage ? debugUnlockStage : PlayerPrefs.GetInt("UnlockedStage", 1);
        isLocked = requiredUnlockStage > currentUnlocked;

        // 잠금 상태에 따라 지정된 하위 오브젝트들을 켜고 끕니다.
        if (objectsToDisableWhenLocked != null)
        {
            foreach (var obj in objectsToDisableWhenLocked)
            {
                if (obj != null) obj.SetActive(!isLocked);
            }
        }

        if (isLocked)
        {
            // 자물쇠 오버레이 추가
            GameObject overlay = new GameObject("LockOverlay");
            overlay.transform.SetParent(this.transform, false);
            
            RectTransform rt = overlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // 가장 앞으로 오도록 설정
            overlay.transform.SetAsLastSibling();
            
            if (lockIconSprite != null)
            {
                Image iconImg = overlay.AddComponent<Image>();
                iconImg.sprite = lockIconSprite;
                iconImg.preserveAspect = false;
                iconImg.raycastTarget = false;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (playerController != null && playerController.IsExecuting()) return;

        // 고스트 이미지 생성
        ghostObj = new GameObject("DragGhost");
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ghostObj.transform.SetParent(canvas.transform, false);
            ghostObj.transform.SetAsLastSibling();
        }

        Image ghostImg = ghostObj.AddComponent<Image>();
        Image myImg = GetComponent<Image>();
        if (myImg != null)
        {
            ghostImg.sprite = myImg.sprite;
            ghostImg.color = new Color(1, 1, 1, 0.7f); // 반투명하게
            ghostImg.raycastTarget = false;
        }

        ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        if (ghostObj != null) Destroy(ghostObj);

        if (playerController != null && !playerController.IsExecuting())
        {
            PlayerController.CommandBlock block = new PlayerController.CommandBlock(commandType);
            block.functionIndex = callFunctionIndex;
            block.conditionObstacle = conditionObstacle;

            playerController.HandleDropInsert(eventData, block);
        }
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (ghostObj != null && ghostRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)ghostObj.transform.parent, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPoint);
            ghostRect.localPosition = localPoint;
        }
    }
}
