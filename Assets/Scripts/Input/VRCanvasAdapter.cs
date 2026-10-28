using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 화면 고정(Overlay) 캔버스를 VR에서만 World Space 패널로 바꿔서 컨트롤러 레이로 조작할 수 있게 한다.
/// 데스크톱에서는 아무것도 하지 않는다. 캔버스(루트) 오브젝트에 붙인다.
/// 패널은 XR Origin의 자식이 되어 XR Origin의 확대 비율을 같이 받으므로, 사용자에게는 현실 크기로 보인다.
/// 패널은 맵이 아니라 사용자의 머리를 따라다닌다(고개를 돌리면 부드럽게 따라오는 HUD 방식).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRCanvasAdapter : MonoBehaviour
{
    [Header("패널 크기/위치 (현실 기준, 미터)")]
    [Tooltip("패널 가로 길이")]
    public float panelWidth = 1.5f;
    [Tooltip("머리에서 앞쪽으로 떨어진 거리")]
    public float distance = 0.8f;
    [Tooltip("시선 중앙보다 아래로 내릴 높이(음수면 위). 0이 아니면 아래쪽 버튼이 시야 밖으로 잘릴 수 있다")]
    public float below = 0f;

    [Tooltip("체크하면 UI를 항상 맵보다 앞에 그린다(맵이 UI를 뚫고 나오지 않음)")]
    public bool alwaysOnTop = true;

    [Header("VR 배치 보정 (캔버스 좌표 단위, 800x450 기준)")]
    [Tooltip("아래쪽 버튼 그룹을 옮길 양. 음수면 더 아래로 내려가 시야 아래쪽에 자리잡는다")]
    public float bottomGroupOffsetY = -40f;
    [Tooltip("아래쪽 그룹에 속하는 UI의 캔버스 기준 경로. 없는 경로는 무시한다(메뉴 캔버스 등)")]
    public string[] bottomGroupPaths = { "InGameUI/Command_Panel", "InGameUI/Play_Panel" };
    [Tooltip("VR에서는 꺼 둘 UI의 캔버스 기준 경로 (화면 가장자리용 그라데이션 등). 없는 경로는 무시한다")]
    public string[] hideInVRPaths = { "gradation" };

    [Header("따라오기")]
    [Tooltip("0이면 패널이 시선에 딱 붙어 항상 같은 위치로 보인다(고개를 기울여도 같이 기울어짐). 0보다 크면 그 속도로 부드럽게 따라온다")]
    public float followSpeed = 0f;
    [Tooltip("followSpeed가 0보다 클 때만 사용. 시선이 이 각도(도) 안에서 움직이면 패널은 가만히 있는다")]
    public float deadZoneAngle = 8f;

    private Canvas _canvas;
    private RectTransform _rt;
    private VRMapViewer _viewer;
    private Camera _cam;
    private Vector3 _dir;      // XR Origin 기준, 머리에서 패널 쪽을 향하는 방향
    private float _snapLeft;   // 기기 추적이 안정될 때까지는 즉시 맞춘다
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

        ApplyLayoutOffsets();
        if (alwaysOnTop) DrawOnTop();

        _snapLeft = 2f;
        _ready = true;
        Place();
    }

    // 맵(3D)이 UI보다 가까운 부분에서 UI를 뚫고 나오지 않도록, UI의 깊이 검사를 꺼서 항상 맨 위에 그린다.
    // 기본 UI 머티리얼은 런타임 복사본이라 에셋이 바뀌지 않고, 나중에 생성되는 Image/Text에도 적용된다.
    private void DrawOnTop()
    {
        const int always = (int)CompareFunction.Always;
        Canvas.GetDefaultCanvasMaterial().SetInt("unity_GUIZTestMode", always);
        Canvas.GetDefaultCanvasTextMaterial().SetInt("unity_GUIZTestMode", always);

        // 이미 만들어진 마스크(Mask) 안쪽 UI는 설정을 바꾸기 전에 복사된 머티리얼을 쓰므로 직접 바꿔 준다.
        // 슬롯처럼 나중에 켜지는 UI도 있어서 한동안 주기적으로 반복한다.
        PatchGraphicMaterials();
        InvokeRepeating(nameof(PatchGraphicMaterials), 0.5f, 1f);
    }

    private void PatchGraphicMaterials()
    {
        const int always = (int)CompareFunction.Always;
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic is TMP_Text text)
            {
                // TextMeshPro는 자체 머티리얼을 쓰므로 오버레이 모드로 바꾼다.
                if (!text.isOverlay) text.isOverlay = true;
                continue;
            }
            graphic.materialForRendering.SetInt("unity_GUIZTestMode", always);
        }
    }

    // 아래쪽 버튼 그룹은 VR에서만 위치를 보정해 시야 아래쪽에 오게 한다.
    // (위쪽 커맨드 패널은 PlayerController가 위치를 직접 관리하므로 여기서 옮기지 않는다)
    private void ApplyLayoutOffsets()
    {
        MoveGroup(bottomGroupPaths, bottomGroupOffsetY);

        // 화면 가장자리용 장식은 VR에서는 필요 없으므로 끈다.
        if (hideInVRPaths == null) return;
        foreach (var path in hideInVRPaths)
        {
            var target = transform.Find(path);
            if (target != null) target.gameObject.SetActive(false);
        }
    }

    private void MoveGroup(string[] paths, float offsetY)
    {
        if (paths == null || Mathf.Approximately(offsetY, 0f)) return;
        foreach (var path in paths)
        {
            var child = transform.Find(path) as RectTransform;
            if (child != null) child.anchoredPosition += new Vector2(0f, offsetY);
        }
    }

    void LateUpdate()
    {
        if (_ready) Place();
    }

    private void Place()
    {
        if (_cam == null) _cam = _viewer.GetComponentInChildren<Camera>();
        if (_cam == null) return;

        // 지금 머리 위치와 시선 방향을 XR Origin 기준(현실 미터)으로 얻는다.
        Transform origin = _viewer.transform;
        Vector3 head = origin.InverseTransformPoint(_cam.transform.position);
        Vector3 forward = origin.InverseTransformDirection(_cam.transform.forward);

        if (_snapLeft > 0f || followSpeed <= 0f)
        {
            _snapLeft -= Time.unscaledDeltaTime;
            _dir = forward;
        }
        else if (Vector3.Angle(_dir, forward) > deadZoneAngle)
        {
            // 시선이 여유 각도를 벗어나면 부드럽게 따라간다.
            float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            _dir = Vector3.Slerp(_dir, forward, t);
        }

        // 시선에 딱 붙는 모드에서는 머리의 위쪽 방향을 그대로 써서, 고개를 기울여도 화면에서 같은 위치/각도로 보이게 한다.
        Vector3 up = followSpeed <= 0f ? origin.InverseTransformDirection(_cam.transform.up) : Vector3.up;

        Vector3 pos = head + _dir.normalized * distance - up * below;
        _rt.localPosition = pos;
        // 사용자를 바라보도록(캔버스의 앞면이 사용자 쪽) 회전
        _rt.localRotation = Quaternion.LookRotation(pos - head, up);
    }
}
