using UnityEngine.XR;

/// <summary>
/// VR로 동작해야 하는지 판단하는 단일 기준.
/// 실제 XR 기기가 켜져 있거나, 에디터에서 메뉴(Freak/VR)로 강제 옵션을 켠 경우 true.
/// 에디터 옵션은 모든 씬에 공통 적용되고 빌드에는 영향을 주지 않는다.
/// </summary>
public static class XRMode
{
    public static bool Wanted
    {
        get
        {
            if (XRSettings.isDeviceActive) return true;
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EditorKey, false);
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR
    private const string EditorKey = "Freak.ForceXRInEditor";
    private const string MenuPath = "Freak/VR/에디터에서 XR 강제";

    [UnityEditor.MenuItem(MenuPath)]
    private static void Toggle()
    {
        UnityEditor.EditorPrefs.SetBool(EditorKey, !UnityEditor.EditorPrefs.GetBool(EditorKey, false));
    }

    [UnityEditor.MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        UnityEditor.Menu.SetChecked(MenuPath, UnityEditor.EditorPrefs.GetBool(EditorKey, false));
        return true;
    }
#endif
}
