using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// VR 미니어처 뷰. XR Origin 루트에 붙인다.
/// 맵을 줄이는 대신 XR Origin을 크게 키워서(거인 시점) 정상 크기의 맵이 테이블 위 모형처럼 보이게 한다.
/// 게임 로직/물리/좌표는 전혀 건드리지 않는다. XR Origin이 비활성(데스크톱)이면 아무 일도 하지 않는다.
/// 맵 위치는 현재 머리(HMD) 위치를 기준으로 잡으므로, 실제 기기와 시뮬레이터 모두 눈높이에 맞게 배치된다.
/// 컨트롤러 스틱을 좌/우로 밀면 맵이 그 방향으로 Y축 회전하는 것처럼 보이도록 XR Origin을 맵 중심 둘레로 공전시킨다.
/// </summary>
public class VRMapViewer : MonoBehaviour
{
    public static VRMapViewer Instance { get; private set; }

    [Header("미니어처 설정 (현실 기준, 미터)")]
    [Tooltip("맵의 긴 변이 현실에서 차지할 길이")]
    public float miniatureSize = 0.8f;
    [Tooltip("눈 높이보다 테이블(맵 바닥)이 얼마나 아래에 있는지")]
    public float tableBelowEyes = 0.5f;
    [Tooltip("머리에서 맵 중심까지의 앞쪽 거리")]
    public float distanceToMap = 0.6f;
    [Tooltip("시작 시 맵을 바라보는 방향(Y 회전각). 0이면 -Z쪽에서 +Z쪽을 정면으로, 30~45면 비스듬히(대각선) 바라봄")]
    public float viewYaw = 40f;
    [Tooltip("기기의 머리 추적이 안정될 때까지 위치를 다시 맞추는 시간(초)")]
    public float settleTime = 2f;

    [Header("따라오기")]
    [Tooltip("체크하면 내가 움직일 때 맵이 부드럽게 따라온다. 기본은 꺼짐(월드 고정이 멀미가 적음)")]
    public bool followHead = false;
    [Tooltip("이 거리(현실 미터)까지는 맵이 가만히 있고, 벗어나면 따라오기 시작 (머리 흔들림으로 어지럽지 않게)")]
    public float followRadius = 0.25f;
    [Tooltip("따라오는 속도(클수록 빠르게 붙음)")]
    public float followSpeed = 3f;

    [Header("스틱 회전")]
    [Tooltip("스틱을 끝까지 밀었을 때 초당 회전 각도")]
    public float rotateSpeed = 90f;
    [Tooltip("이 값보다 작은 스틱 입력은 무시")]
    public float deadZone = 0.2f;
    [Tooltip("체크하면 스틱 방향과 맵 회전 방향이 반대가 됨")]
    public bool invertRotation = false;

    /// <summary>XR Origin 기준(현실 미터) 사용자가 서 있는 기준 머리 위치. UI 배치에 사용.</summary>
    public Vector3 HeadRef => _headRef;

    private bool _hasMap;
    private Vector3 _mapCenter;
    private float _scale = 1f;
    private float _settleLeft;
    private float _yaw;
    private Vector3 _headRef;      // 회전 중 기준점이 흔들리지 않도록 고정해 두는 머리 위치

    private InputAction _leftStick;
    private InputAction _rightStick;

    void OnEnable()
    {
        Instance = this;
        _yaw = viewYaw;
        _settleLeft = settleTime;

        // 실제 OpenXR 컨트롤러와 XR Device Simulator 모두 XRController 레이아웃으로 잡힌다.
        _leftStick = new InputAction("MapRotateLeft", InputActionType.Value, "<XRController>{LeftHand}/{Primary2DAxis}");
        _rightStick = new InputAction("MapRotateRight", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
        _leftStick.Enable();
        _rightStick.Enable();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        _leftStick?.Disable(); _leftStick?.Dispose();
        _rightStick?.Disable(); _rightStick?.Dispose();
    }

    /// <summary>MapGenerator가 맵 생성을 마친 뒤 호출한다.</summary>
    public void Align(Vector3 mapCenter, float mapWidth, float mapDepth)
    {
        float longSide = Mathf.Max(mapWidth, mapDepth);
        _scale = Mathf.Max(0.01f, longSide / Mathf.Max(0.01f, miniatureSize));
        _mapCenter = mapCenter;
        _hasMap = true;
        _settleLeft = settleTime;

        UpdateHeadRef();
        Apply();
    }

    void Update()
    {
        // 맵이 없는 씬(메뉴)에서도 UI 배치용 머리 기준 위치는 잡아 둔다.
        if (!_hasMap)
        {
            if (_settleLeft > 0f)
            {
                _settleLeft -= Time.unscaledDeltaTime;
                UpdateHeadRef();
            }
            return;
        }

        // 기기 추적 시작 직후에는 머리 위치가 아직 (0,0,0)일 수 있어서, 잠시 동안 계속 다시 맞춘다.
        if (_settleLeft > 0f)
        {
            _settleLeft -= Time.unscaledDeltaTime;
            UpdateHeadRef();
        }

        // 좌/우 스틱 중 더 크게 밀린 쪽의 X값으로 회전
        float x = _leftStick.ReadValue<Vector2>().x;
        float xr = _rightStick.ReadValue<Vector2>().x;
        if (Mathf.Abs(xr) > Mathf.Abs(x)) x = xr;

        bool following = followHead && _settleLeft <= 0f && FollowHeadStep();

        bool rotating = Mathf.Abs(x) > deadZone;
        if (rotating)
        {
            // 스틱 오른쪽 = XR Origin이 시계 방향으로 공전 = 맵이 오른쪽으로 돌아오는 것처럼 보임
            float dir = invertRotation ? -1f : 1f;
            _yaw += x * dir * rotateSpeed * Time.unscaledDeltaTime;
        }

        if (_settleLeft > 0f || rotating || following) Apply();
    }

    /// <summary>머리가 기준점에서 followRadius 밖으로 벗어났으면 기준점을 부드럽게 끌어당긴다. 움직였으면 true.</summary>
    private bool FollowHeadStep()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam == null) return false;

        Vector3 head = transform.InverseTransformPoint(cam.transform.position);
        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        bool moved = false;

        // 수평(XZ): 반경을 벗어난 만큼만 끌어당김
        Vector3 d = head - _headRef;
        d.y = 0f;
        if (d.magnitude > followRadius)
        {
            _headRef += d.normalized * ((d.magnitude - followRadius) * t);
            moved = true;
        }

        // 높이(Y): 앉거나 일어서면 눈높이에 맞춰 따라감
        float dy = head.y - _headRef.y;
        if (Mathf.Abs(dy) > followRadius * 0.5f)
        {
            _headRef.y += (dy - Mathf.Sign(dy) * followRadius * 0.5f) * t;
            moved = true;
        }
        return moved;
    }

    private void UpdateHeadRef()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam != null) _headRef = transform.InverseTransformPoint(cam.transform.position);
    }

    private void Apply()
    {
        transform.localScale = Vector3.one * _scale;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        // 맵 중심이 (머리 앞 distanceToMap, 눈 아래 tableBelowEyes)에 오도록 Origin 위치를 계산.
        // 같은 기준점(_headRef)을 쓰므로 회전하면 맵 중심 둘레를 도는 공전이 된다.
        Vector3 mapLocal = new Vector3(_headRef.x, _headRef.y - tableBelowEyes, _headRef.z + distanceToMap);
        transform.position = _mapCenter - transform.rotation * (mapLocal * _scale);
    }
}
