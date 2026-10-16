using UnityEngine;

/// <summary>
/// 전역 입력 접근점. 기본은 DesktopInput이며, VR/모바일 등에서는 Current를 교체한다.
/// </summary>
public static class GameInput
{
    public static IGameInput Current { get; set; } = new DesktopInput();

    public static Vector2 PointerPosition => Current.PointerPosition;
    public static bool PointerDown(int button = 0) => Current.PointerDown(button);
    public static bool Pointer(int button = 0) => Current.Pointer(button);
    public static bool PointerUp(int button = 0) => Current.PointerUp(button);

    public static bool AnyKeyDown => Current.AnyKeyDown;
    public static bool KeyDown(KeyCode key) => Current.KeyDown(key);
    public static float GetAxisRaw(string axisName) => Current.GetAxisRaw(axisName);
}
