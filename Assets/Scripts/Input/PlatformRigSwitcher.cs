using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 씬 시작 시 VR 기기 사용 여부에 따라 데스크톱용 오브젝트와 XR 오브젝트 중 한쪽만 활성화한다.
/// desktopObjects: 기존 Main Camera, 데스크톱 전용 UI 등 / xrObjects: XR Origin, XR 전용 UI 등
/// 두 그룹 모두 씬에 둔 채로 이 컴포넌트를 아무 오브젝트에나 하나 붙여 사용한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class PlatformRigSwitcher : MonoBehaviour
{
    [Tooltip("데스크톱(Windows/Mac)에서만 켜질 오브젝트들")]
    public GameObject[] desktopObjects;

    [Tooltip("VR 기기가 있을 때만 켜질 오브젝트들")]
    public GameObject[] xrObjects;

    [Tooltip("에디터에서 헤드셋 없이 VR 구성을 강제로 확인하고 싶을 때 체크")]
    public bool forceXRInEditor = false;

    /// <summary>현재 VR(XR) 모드로 실행 중인지. Awake에서 결정된다.</summary>
    public static bool IsXR { get; internal set; }

    void Awake()
    {
        bool useXR = XRMode.Wanted;
#if UNITY_EDITOR
        useXR |= forceXRInEditor;
#endif
        IsXR = useXR;

        foreach (var go in desktopObjects) if (go != null) go.SetActive(!useXR);
        foreach (var go in xrObjects) if (go != null) go.SetActive(useXR);
    }
}
