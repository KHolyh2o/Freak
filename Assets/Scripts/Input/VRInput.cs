using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Quest 등 XR 컨트롤러 기반 구현 (UnityEngine.XR.InputDevices 사용).
/// 오른손 트리거 = 포인터 0(선택), 오른손 그립 = 포인터 1,
/// 오른손 A(primary) = Space, 왼손 메뉴 = Escape, 왼손 스틱 = Horizontal/Vertical.
/// 월드 스페이스 UI 조작은 XR Interaction Toolkit(XRUIInputModule)이 담당한다.
/// </summary>
public class VRInput : IGameInput
{
    // 프레임당 한 번만 읽고, 이전 프레임 값과 비교해 Down/Up을 계산한다.
    private class ButtonState
    {
        public bool Current;
        public bool Previous;
        public int Frame = -1;
    }

    private readonly Dictionary<string, ButtonState> _states = new Dictionary<string, ButtonState>();

    private static InputDevice Device(XRNode node) => InputDevices.GetDeviceAtXRNode(node);

    private ButtonState Poll(string id, XRNode node, InputFeatureUsage<bool> usage)
    {
        if (!_states.TryGetValue(id, out var s))
        {
            s = new ButtonState();
            _states[id] = s;
        }

        if (s.Frame != Time.frameCount)
        {
            s.Previous = s.Current;
            s.Current = Device(node).TryGetFeatureValue(usage, out bool v) && v;
            s.Frame = Time.frameCount;
        }
        return s;
    }

    private ButtonState Trigger => Poll("trigger", XRNode.RightHand, CommonUsages.triggerButton);
    private ButtonState Grip => Poll("grip", XRNode.RightHand, CommonUsages.gripButton);
    private ButtonState A => Poll("a", XRNode.RightHand, CommonUsages.primaryButton);
    private ButtonState Menu => Poll("menu", XRNode.LeftHand, CommonUsages.menuButton);

    private ButtonState PointerButton(int button) => button == 0 ? Trigger : Grip;

    // 오른손이 가리키는 방향 2m 앞 지점을 화면 좌표로 환산 (카메라 레이캐스트 기반 기능용 근사값)
    public Vector2 PointerPosition
    {
        get
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;

            var dev = Device(XRNode.RightHand);
            if (dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot) &&
                cam.transform.parent != null)
            {
                Transform origin = cam.transform.parent; // XR Origin(Camera Offset) 기준 로컬 → 월드 변환
                Vector3 worldPos = origin.TransformPoint(pos);
                Vector3 worldFwd = origin.TransformDirection(rot * Vector3.forward);
                return cam.WorldToScreenPoint(worldPos + worldFwd * 2f);
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }

    public bool PointerDown(int button) { var s = PointerButton(button); return s.Current && !s.Previous; }
    public bool Pointer(int button) => PointerButton(button).Current;
    public bool PointerUp(int button) { var s = PointerButton(button); return !s.Current && s.Previous; }

    public bool AnyKeyDown
    {
        get
        {
            var t = Trigger; var a = A; var m = Menu;
            return (t.Current && !t.Previous) || (a.Current && !a.Previous) || (m.Current && !m.Previous);
        }
    }

    public bool KeyDown(KeyCode key)
    {
        ButtonState s;
        switch (key)
        {
            case KeyCode.Space: s = A; break;
            case KeyCode.Escape: s = Menu; break;
            default: return false;
        }
        return s.Current && !s.Previous;
    }

    public float GetAxisRaw(string axisName)
    {
        if (!Device(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
            return 0f;

        float v = axisName == "Horizontal" ? stick.x : axisName == "Vertical" ? stick.y : 0f;
        return Mathf.Abs(v) < 0.2f ? 0f : Mathf.Sign(v); // GetAxisRaw처럼 -1/0/1로 정규화(데드존 0.2)
    }
}
