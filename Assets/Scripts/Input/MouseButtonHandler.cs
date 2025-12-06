using UnityEngine;
using UnityEngine.EventSystems; // 이게 꼭 있어야 클릭을 감지합니다.
using UnityEngine.Events;       // 이게 꼭 있어야 UnityEvent를 씁니다.

public class MouseButtonHandler : MonoBehaviour, IPointerClickHandler
{
    // ★ 이 두 줄이 없어서 오류가 난 것입니다!
    public UnityEvent onLeftClick;
    public UnityEvent onRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 좌클릭 이벤트 실행
            if (onLeftClick != null) onLeftClick.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭 이벤트 실행
            if (onRightClick != null) onRightClick.Invoke();
        }
    }
}
