using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 첫 씬이 로드된 직후 XR 기기가 활성화되어 있으면 GameInput을 VRInput으로 교체한다.
/// 데스크톱 빌드나 에디터(XR 미사용)에서는 기본 DesktopInput을 그대로 사용한다.
/// </summary>
public static class InputBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (XRSettings.isDeviceActive)
        {
            GameInput.Current = new VRInput();
            Debug.Log("[InputBootstrap] XR 기기 감지 → VRInput 사용");
        }
        else
        {
            GameInput.Current = new DesktopInput();
        }
    }
}
