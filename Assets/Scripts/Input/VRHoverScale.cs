using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// VR 컨트롤러 레이가 버튼 위에 올라가면 버튼을 살짝 키워서 조준 중임을 보여 준다.
/// VRCanvasAdapter가 VR일 때만 버튼에 자동으로 붙인다.
/// </summary>
public class VRHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scale = 1.12f;

    private Vector3 _baseScale;
    private bool _hovering;
    private Selectable _selectable;

    void Awake()
    {
        _baseScale = transform.localScale;
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hovering || (_selectable != null && !_selectable.interactable)) return;
        _hovering = true;
        _baseScale = transform.localScale;
        transform.localScale = _baseScale * scale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Restore();
    }

    void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (!_hovering) return;
        _hovering = false;
        transform.localScale = _baseScale;
    }
}
