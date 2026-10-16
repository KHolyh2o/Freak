using UnityEngine;

/// <summary>
/// 게임에서 사용하는 입력 추상화. 플랫폼(데스크톱/터치/VR)별로 구현체를 교체할 수 있다.
/// </summary>
public interface IGameInput
{
    Vector2 PointerPosition { get; }
    bool PointerDown(int button);
    bool Pointer(int button);
    bool PointerUp(int button);

    bool AnyKeyDown { get; }
    bool KeyDown(KeyCode key);
    float GetAxisRaw(string axisName);
}
