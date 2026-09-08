using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class SlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerController playerController;
    public GameObject panel;
    public List<PlayerController.CommandBlock> commandList;
    public int slotIndex;

    private GameObject ghostObj;
    private RectTransform ghostRect;
    private CanvasGroup myCanvasGroup;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (playerController != null && playerController.IsExecuting()) return;
        if (commandList == null || slotIndex >= commandList.Count) return; // 여분 빈칸 드래그 방지
        if (commandList[slotIndex].type == PlayerController.CommandType.ScopeEnd) return; // ScopeEnd(빨간/파란 점선 빈칸) 드래그 방지

        // 원본을 반투명하게 하거나 숨기기
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null) myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        myCanvasGroup.alpha = 0.3f;

        // 고스트 생성
        ghostObj = new GameObject("SlotDragGhost");
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
            ghostImg.color = myImg.color;
            ghostImg.raycastTarget = false;
        }

        ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostObj == null) return; // 드래그 취소된 상태
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostObj == null) return; // 드래그 취소된 상태

        if (myCanvasGroup != null) myCanvasGroup.alpha = 1f;
        if (ghostObj != null) Destroy(ghostObj);

        if (playerController != null && !playerController.IsExecuting())
        {
            playerController.HandleDropSlot(eventData, panel, commandList, slotIndex);
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
