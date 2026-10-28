using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 화면 고정(Overlay) 캔버스를 VR에서만 World Space 패널로 바꿔서 컨트롤러 레이로 조작할 수 있게 한다.
/// 데스크톱에서는 아무것도 하지 않는다. 캔버스(루트) 오브젝트에 붙인다.
/// 패널은 XR Origin의 자식이 되어 XR Origin의 확대 비율을 같이 받으므로, 사용자에게는 현실 크기로 보인다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRCanvasAdapter : MonoBehaviour
{
    [Header("패널 크기/위치 (현실 기준, 미터)")]
    [Tooltip("패널 가로 길이")]
    public float panelWidth = 0.7f;
    [Tooltip("기준 머리 위치에서 앞쪽으로 떨어진 거리")]
    public float distance = 0.55f;
    [Tooltip("기준 머리 위치보다 아래로 내릴 높이(음수면 위)")]
    public float below = 0.1f;

    private Canvas _canvas;
    private RectTransform _rt;
    private VRMapViewer _viewer;
    private bool _ready;

    IEnumerator Start()
    {
        if (!PlatformRigSwitcher.IsXR) { enabled = false; yield break; }

        // 캔버스 크기가 첫 프레임에 확정되므로 한 프레임 기다린다.
        yield return null;

        _viewer = VRMapViewer.Instance;
        if (_viewer == null)
        {
            Debug.LogWarning("[VRCanvasAdapter] VRMapViewer(XR Origin)를 찾지 못해 World Space 변환을 건너뜁니다.");
            yield break;
        }

        _canvas = GetComponent<Canvas>();
        _rt = (RectTransform)transform;

        Vector2 size = _rt.rect.size;
        if (size.x < 1f || size.y < 1f) size = new Vector2(1920f, 1080f);

        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        _rt.SetParent(_viewer.transform, false);
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = size;
        _rt.localScale = Vector3.one * (panelWidth / size.x);

        if (GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        _ready = true;
        Place();
    }

    void LateUpdate()
    {
        if (_ready) Place();
    }

    private void Place()
    {
        Vector3 head = _viewer.HeadRef;
        Vector3 pos = head + new Vector3(0f, -below, distance);
        _rt.localPosition = pos;
        // 사용자를 바라보도록(캔버스의 앞면이 사용자 쪽) 회전
        _rt.localRotation = Quaternion.LookRotation(pos - head, Vector3.up);
    }
}
