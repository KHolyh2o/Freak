using UnityEngine;

/// <summary>
/// 마우스/키보드(레거시 Input) 기반 구현. 터치가 있으면 첫 터치를 포인터 위치로 사용한다.
/// </summary>
public class DesktopInput : IGameInput
{
    public Vector2 PointerPosition =>
        Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

    public bool PointerDown(int button) => Input.GetMouseButtonDown(button);
    public bool Pointer(int button) => Input.GetMouseButton(button);
    public bool PointerUp(int button) => Input.GetMouseButtonUp(button);

    public bool AnyKeyDown => Input.anyKeyDown;
    public bool KeyDown(KeyCode key) => Input.GetKeyDown(key);
    public float GetAxisRaw(string axisName) => Input.GetAxisRaw(axisName);
}
