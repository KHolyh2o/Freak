using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommandDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerController.CommandType commandType;
    public int callFunctionIndex = -1;
    public PlayerController.ObstacleType conditionObstacle = PlayerController.ObstacleType.None;

    private GameObject ghostObj;
    private RectTransform ghostRect;
    private PlayerController playerController;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
