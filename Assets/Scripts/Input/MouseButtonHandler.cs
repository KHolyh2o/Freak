using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MouseButtonHandler : MonoBehaviour, IPointerDownHandler
{
    public UnityEvent onLeftClick = new UnityEvent();
    public UnityEvent onRightClick = new UnityEvent();

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[MouseButtonHandler] 마우스 눌림 감지! 버튼 종류: {eventData.button}, 대상: {gameObject.name}");

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (onLeftClick != null) onLeftClick.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (onRightClick != null) onRightClick.Invoke();
        }
    }
}
