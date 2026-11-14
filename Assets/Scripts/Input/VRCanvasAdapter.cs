using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    // 패널 거리/가로 길이는 XR_Set 프리팹의 VRMapViewer(uiDistance, uiPanelWidth)에서 조절한다.
    [Header("패널 위치 (현실 기준, 미터)")]
    [Tooltip("시선 중앙보다 아래로 내릴 높이(음수면 위). 0이 아니면 아래쪽 버튼이 시야 밖으로 잘릴 수 있다")]
    public float below = 0f;

    [Tooltip("체크하면 UI를 항상 맵보다 앞에 그린다(맵이 UI를 뚫고 나오지 않음)")]
    public bool alwaysOnTop = true;

    [Tooltip("플레이 중 패널 중심을 시선보다 아래로 기울이는 각도(도). 위쪽 UI가 잘려 보이면 키운다")]
    public float pitchDown = 10f;

    [Header("VR 배치 보정 (캔버스 좌표 단위, 800x450 기준)")]
    [Tooltip("VR 패널의 가로:세로 비율. 화면에 꽉 차게 늘어나는 UI가 위아래로 길어지지 않도록 이 비율로 고정한다")]
    public float vrAspect = 16f / 9f;
    [Tooltip("아래쪽 버튼 그룹을 옮길 양. 음수면 더 아래로 내려가 시야 아래쪽에 자리잡는다")]
    public float bottomGroupOffsetY = -40f;
    [Tooltip("아래쪽 그룹에 속하는 UI의 캔버스 기준 경로. 없는 경로는 무시한다(메뉴 캔버스 등)")]
    public string[] bottomGroupPaths = { "InGameUI/Command_Panel", "InGameUI/Play_Panel" };
    [Tooltip("VR에서는 꺼 둘 UI의 캔버스 기준 경로 (화면 가장자리용 그라데이션 등). 없는 경로는 무시한다")]
    public string[] hideInVRPaths = { "gradation" };

    [Header("따라오기")]
    [Tooltip("체크하면 메뉴 씬(Intro/Start/main)에서도 시선을 따라온다. 기본은 꺼짐: 메뉴는 처음 바라본 정면에 고정")]
    public bool followInMenuScenes = true;
    [Tooltip("0이면 패널이 시선에 딱 붙어 항상 같은 위치로 보인다(고개를 기울여도 같이 기울어짐). 0보다 크면 그 속도로 부드럽게 따라온다")]
    public float followSpeed = 0f;
    [Tooltip("followSpeed가 0보다 클 때만 사용. 시선이 이 각도(도) 안에서 움직이면 패널은 가만히 있는다")]
    public float deadZoneAngle = 8f;

    [Tooltip("메뉴 씬에서 설정창(SettingsUI)이 든 캔버스를 사용자 쪽으로 당기는 거리(미터). 메뉴 캔버스와 같은 평면에 겹쳐 레이가 뒤쪽 패널에 맞는 것을 막는다. 0이면 끈다")]
    public float menuPopupNudge = 0.05f;

    private Canvas _canvas;
    private RectTransform _rt;
    private VRMapViewer _viewer;
    private Camera _cam;
    private Vector3 _dir;      // XR Origin 기준, 머리에서 패널 쪽을 향하는 방향
    private float _snapLeft;   // 기기 추적이 안정될 때까지는 즉시 맞춘다
    private bool _ready;
    private bool _followHead;  // false면(메뉴 씬) 기기 추적이 안정된 뒤의 정면에 고정
    private Vector3 _fixedHead;
    private Vector2 _size;
    private float _depthNudge; // 메뉴 씬에서 설정창 캔버스를 메뉴 캔버스 앞으로 당기는 거리(미터)
    private SettingsUI _settingsUI;     // 진단용: 설정창이 열리는 순간을 감지한다
    private bool _settingsWasOn;

    IEnumerator Start()
    {
        if (!PlatformRigSwitcher.IsXR) { enabled = false; yield break; }

        // 캔버스 크기가 첫 프레임에 확정되므로 한 프레임 기다린다.
        yield return null;

        // 씬 이동 전에 소속 씬을 알아 둔다(아래에서 XR Origin의 자식이 된다).
        _followHead = followInMenuScenes || !SceneIds.IsMenu(gameObject.scene.name);
        _settingsUI = GetComponentInChildren<SettingsUI>(true);
        _depthNudge = _settingsUI != null ? menuPopupNudge : 0f;

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

        // 헤드셋 화면은 거의 정사각형이라 캔버스 높이가 데스크톱(16:9)보다 훨씬 커진다. 화면에 꽉 차게 늘어나도록
        // 만든 UI(AI 팝업 등)가 위아래로 길어지므로, 가로는 그대로 두고 높이를 16:9로 고정한다.
        size.y = size.x / vrAspect;

        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
        Debug.Log($"[VRCanvasAdapter] '{name}' 월드 스페이스로 변환 (크기 {size}, 카메라 {(Camera.main != null ? Camera.main.name : "없음")})");

        _rt.SetParent(_viewer.transform, false);
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = size;
        _size = size;
        _rt.localScale = Vector3.one * (_viewer.uiPanelWidth / size.x);

        if (GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        ApplyLayoutOffsets();
        if (alwaysOnTop) DrawOnTop();

        // 슬롯처럼 나중에 생기는 버튼도 있어서 주기적으로 반복한다.
        AddHoverScale();
        InvokeRepeating(nameof(AddHoverScale), 1f, 1f);

        _snapLeft = 2f;
        _ready = true;
        Place();
    }

    // 레이를 올린 버튼이 살짝 커지게 한다. 자체 호버 효과(EventTrigger)가 있는 버튼은 서로 겹치지 않게 건너뛴다.
    private void AddHoverScale()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<VRHoverScale>() != null || button.GetComponent<EventTrigger>() != null) continue;
            button.gameObject.AddComponent<VRHoverScale>();
        }
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
        if (_ready) EnsureWorldSpace();
        if (_ready) Place();

        if (_settingsUI != null)
        {
            bool on = _settingsUI.gameObject.activeInHierarchy;
            if (on && !_settingsWasOn) LogDiagnostics();
            _settingsWasOn = on;
        }
    }

    // 변환한 뒤 누군가(원인 미상) 캔버스를 다시 화면 고정(Overlay)으로 돌리면, VR에서는 눈마다 평면으로 그려져
    // 초점이 안 맞고 두 개로 보인다. 되돌려진 것을 감지하면 월드 스페이스와 크기를 다시 맞춘다.
    private void EnsureWorldSpace()
    {
        if (_canvas.renderMode == RenderMode.WorldSpace) return;

        Debug.LogWarning($"[VRCanvasAdapter] '{name}' 렌더 모드가 {_canvas.renderMode}(으)로 되돌아가 있어 월드 스페이스로 다시 바꿉니다 (씬={gameObject.scene.name}, 프레임={Time.frameCount}, 크기={_rt.rect.size})");
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = _size;
    }

    // 설정창이 열린 순간의 캔버스/카메라 상태를 로그로 남긴다(VR에서 설정창이 이상하게 보이는 원인 조사용).
    private void LogDiagnostics()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[VRCanvasDiag] 설정창 열림 씬={gameObject.scene.name} 따라오기={_followHead} 당김={_depthNudge}");

        Camera cam = _viewer != null ? _viewer.GetComponentInChildren<Camera>() : null;
        sb.AppendLine($"[VRCanvasDiag] 카메라={(cam != null ? cam.name : "없음")} 위치={(cam != null ? cam.transform.position.ToString("F2") : "-")} Camera.main={(Camera.main != null ? Camera.main.name : "없음")} 켜진 카메라 수={Camera.allCamerasCount}");

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var rt = (RectTransform)canvas.transform;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, rt.position) : -1f;
            int activeChildren = 0;
            var names = new System.Text.StringBuilder();
            foreach (Transform child in canvas.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                activeChildren++;
                names.Append(child.name).Append(',');
            }
            sb.AppendLine($"[VRCanvasDiag] 캔버스 '{canvas.name}' 모드={canvas.renderMode} 부모={(canvas.transform.parent != null ? canvas.transform.parent.name : "없음")} " +
                          $"월드위치={rt.position.ToString("F2")} 카메라까지={dist:F2}m 월드가로={rt.rect.width * rt.lossyScale.x:F2}m " +
                          $"크기={rt.rect.size} 정렬={canvas.sortingOrder} 켜진자식={activeChildren}[{names}]");
        }

        var panel = _settingsUI.settingsPanel != null ? (RectTransform)_settingsUI.settingsPanel.transform : null;
        if (panel != null)
        {
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            float d = cam != null ? Vector3.Distance(cam.transform.position, panel.position) : -1f;
            sb.AppendLine($"[VRCanvasDiag] 설정패널 카메라까지={d:F2}m 월드가로={Vector3.Distance(corners[0], corners[3]):F2}m 월드높이={Vector3.Distance(corners[0], corners[1]):F2}m 로컬z={panel.localPosition.z:F2}");
        }

        Debug.Log(sb.ToString());
    }

    private void Place()
    {
        if (_cam == null) _cam = _viewer.GetComponentInChildren<Camera>();
        if (_cam == null) return;

        // 지금 머리 위치와 시선 방향을 XR Origin 기준(현실 미터)으로 얻는다.
        Transform origin = _viewer.transform;
        Vector3 head = origin.InverseTransformPoint(_cam.transform.position);
        Vector3 forward = origin.InverseTransformDirection(_cam.transform.forward);

        if (!_followHead)
        {
            // 메뉴: 추적이 안정될 때까지만 정면을 맞추고, 그 뒤로는 그 자리에 고정한다.
            if (_snapLeft > 0f)
            {
                _snapLeft -= Time.unscaledDeltaTime;
                Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up);
                _dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
                _fixedHead = head;
            }
            float dist = _viewer.uiDistance - _depthNudge;
            Vector3 fixedPos = _fixedHead + _dir * dist - Vector3.up * below;
            // 가까워진 만큼 작게 해서 화면에서 보이는 크기는 메뉴 캔버스와 같게 유지한다.
            _rt.localScale = Vector3.one * (_viewer.uiPanelWidth / _size.x) * (dist / _viewer.uiDistance);
            _rt.localPosition = fixedPos;
            _rt.localRotation = Quaternion.LookRotation(_dir, Vector3.up);
            return;
        }

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

        // 인스펙터에서 값을 바꿔도 바로 반영되도록 매 프레임 크기를 맞춘다.
        float followDist = _viewer.uiDistance - _depthNudge;
        _rt.localScale = Vector3.one * (_viewer.uiPanelWidth / _size.x) * (followDist / _viewer.uiDistance);

        // 헤드셋은 위쪽 시야가 좁고 착용 위치에 따라 위가 잘리므로, 패널 중심을 시선보다 살짝 아래로 내린다.
        Vector3 dir = _dir.normalized;
        Vector3 right = Vector3.Cross(up, dir);
        if (right.sqrMagnitude > 0.0001f) dir = Quaternion.AngleAxis(pitchDown, right.normalized) * dir;

        Vector3 pos = head + dir * followDist - up * below;
        _rt.localPosition = pos;
        // 사용자를 바라보도록(캔버스의 앞면이 사용자 쪽) 회전
        _rt.localRotation = Quaternion.LookRotation(pos - head, up);
    }
}
